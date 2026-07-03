using LooksRatingApi.Contracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;

namespace LooksRatingApi.Infrastructure.DeployMigrations
{
    /// <summary>
    /// One-time data migration: copy photo profiles from «Потный июнь» to «Обгоревший июль»
    /// with zero ratings (new profiles, same photos).
    /// </summary>
    public sealed class CopyJuneToJulyPhotoProfilesDeployMigration : IDeployMigration
    {
        public const string SourceSeasonIdValue = "2c081626-23e1-4740-a871-fac8a97519be";
        public const string TargetSeasonIdValue = "93ee80fe-cae5-4e44-8e03-d8eea253acb9";

        public static readonly Guid SourceSeasonId = Guid.Parse(SourceSeasonIdValue);
        public static readonly Guid TargetSeasonId = Guid.Parse(TargetSeasonIdValue);

        public string Name =>
            $"copy-photo-profiles:{SourceSeasonIdValue}:{TargetSeasonIdValue}";

        private const int BatchSize = 500;

        private readonly LooksRatingDbContext _context;
        private readonly IDatabase _redis;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly ILogger<CopyJuneToJulyPhotoProfilesDeployMigration> _logger;

        public CopyJuneToJulyPhotoProfilesDeployMigration(
            LooksRatingDbContext context,
            IConnectionMultiplexer redis,
            INormalizeCityNameService normalizeCityNameService,
            ILogger<CopyJuneToJulyPhotoProfilesDeployMigration> logger)
        {
            _context = context;
            _redis = redis.GetDatabase();
            _normalizeCityNameService = normalizeCityNameService;
            _logger = logger;
        }

        public async Task<bool> ApplyAsync(CancellationToken cancellationToken = default)
        {
            var sourceSeasonExists = await _context.Seasons
                .AsNoTracking()
                .AnyAsync(s => s.Id == SourceSeasonId, cancellationToken);

            var targetSeasonExists = await _context.Seasons
                .AsNoTracking()
                .AnyAsync(s => s.Id == TargetSeasonId, cancellationToken);

            if (!sourceSeasonExists || !targetSeasonExists)
            {
                _logger.LogWarning(
                    "Season photo profile copy skipped: sourceExists={SourceExists}, targetExists={TargetExists}",
                    sourceSeasonExists,
                    targetSeasonExists);
                return false;
            }

            var existingTargetUserIds = await _context.PhotoProfiles
                .AsNoTracking()
                .Where(p => p.SeasonId == TargetSeasonId)
                .Select(p => p.UserId)
                .ToHashSetAsync(cancellationToken);

            var skip = 0;
            var copiedProfiles = 0;
            var copiedPhotos = 0;
            var skippedUsers = 0;

            while (true)
            {
                var sourceProfiles = await _context.PhotoProfiles
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Include(p => p.Photos)
                    .Where(p => p.SeasonId == SourceSeasonId)
                    .Where(p => p.Status == StatusEnum.Active || p.Status == StatusEnum.Archived)
                    .OrderBy(p => p.Id)
                    .Skip(skip)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken);

                if (sourceProfiles.Count == 0)
                {
                    break;
                }

                var batch = new List<(PhotoProfile Profile, User User)>();

                foreach (var source in sourceProfiles)
                {
                    if (existingTargetUserIds.Contains(source.UserId))
                    {
                        skippedUsers++;
                        continue;
                    }

                    var mapped = TryCreateTargetProfile(source);
                    if (mapped is null)
                    {
                        continue;
                    }

                    _context.PhotoProfiles.Add(mapped.Value.Profile);
                    batch.Add(mapped.Value);
                }

                if (batch.Count > 0)
                {
                    var saved = await SaveBatchAsync(batch, existingTargetUserIds, cancellationToken);
                    copiedProfiles += saved.Profiles;
                    copiedPhotos += saved.Photos;
                    skippedUsers += saved.SkippedUsers;
                }

                _context.ChangeTracker.Clear();
                skip += BatchSize;

                if (sourceProfiles.Count < BatchSize)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Season photo profile copy finished: profiles={Profiles}, photos={Photos}, skippedUsers={SkippedUsers}",
                copiedProfiles,
                copiedPhotos,
                skippedUsers);

            return true;
        }

        private (PhotoProfile Profile, User User)? TryCreateTargetProfile(PhotoProfile source)
        {
            if (source.User is null)
            {
                _logger.LogWarning(
                    "Skipping profile {ProfileId}: user navigation is missing",
                    source.Id);
                return null;
            }

            if (source.Photos.Count == 0)
            {
                _logger.LogWarning(
                    "Skipping user {UserId}: source profile {ProfileId} has no photos",
                    source.UserId,
                    source.Id);
                return null;
            }

            var cityResult = CityVo.Create(source.CityNomination.Value ?? string.Empty);
            if (cityResult.IsFailure)
            {
                _logger.LogWarning(
                    "Skipping user {UserId}: source profile {ProfileId} has invalid city",
                    source.UserId,
                    source.Id);
                return null;
            }

            var now = DateTime.UtcNow;
            var newProfile = new PhotoProfile
            {
                Id = Guid.NewGuid(),
                UserId = source.UserId,
                SeasonId = TargetSeasonId,
                Rating = 0m,
                RatingCount = 0,
                Rank = RankEnum.Terrible,
                Status = StatusEnum.Active,
                AgeNomination = source.AgeNomination,
                GenderNomination = source.GenderNomination,
                CityNomination = cityResult.Value,
                CreatedAt = now,
            };

            foreach (var sourcePhoto in source.Photos.OrderBy(photo => photo.SortOrder))
            {
                newProfile.Photos.Add(new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    PhotoProfileId = newProfile.Id,
                    TelegramFileId = sourcePhoto.TelegramFileId,
                    SortOrder = sourcePhoto.SortOrder,
                    CreatedAt = now,
                });
            }

            return (newProfile, source.User);
        }

        private async Task<(int Profiles, int Photos, int SkippedUsers)> SaveBatchAsync(
            List<(PhotoProfile Profile, User User)> batch,
            HashSet<Guid> existingTargetUserIds,
            CancellationToken cancellationToken)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Unique constraint hit while copying photo profiles batch; retrying per profile");
                _context.ChangeTracker.Clear();
                await ReloadExistingTargetUserIdsAsync(existingTargetUserIds, cancellationToken);
                return await SaveProfilesOneByOneAsync(batch, existingTargetUserIds, cancellationToken);
            }

            foreach (var (profile, _) in batch)
            {
                existingTargetUserIds.Add(profile.UserId);
            }

            foreach (var (profile, user) in batch)
            {
                await TryPopulateRedisAsync(profile, user, cancellationToken);
            }

            return (
                batch.Count,
                batch.Sum(item => item.Profile.Photos.Count),
                0);
        }

        private async Task<(int Profiles, int Photos, int SkippedUsers)> SaveProfilesOneByOneAsync(
            IReadOnlyList<(PhotoProfile Profile, User User)> batch,
            HashSet<Guid> existingTargetUserIds,
            CancellationToken cancellationToken)
        {
            var profiles = 0;
            var photos = 0;
            var skippedUsers = 0;

            foreach (var (profile, user) in batch)
            {
                if (existingTargetUserIds.Contains(profile.UserId))
                {
                    skippedUsers++;
                    continue;
                }

                _context.PhotoProfiles.Add(profile);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    existingTargetUserIds.Add(profile.UserId);
                    await TryPopulateRedisAsync(profile, user, cancellationToken);
                    profiles++;
                    photos += profile.Photos.Count;
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    existingTargetUserIds.Add(profile.UserId);
                    skippedUsers++;
                }
                finally
                {
                    _context.ChangeTracker.Clear();
                }
            }

            return (profiles, photos, skippedUsers);
        }

        private async Task ReloadExistingTargetUserIdsAsync(
            HashSet<Guid> existingTargetUserIds,
            CancellationToken cancellationToken)
        {
            var userIds = await _context.PhotoProfiles
                .AsNoTracking()
                .Where(p => p.SeasonId == TargetSeasonId)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

            existingTargetUserIds.Clear();
            foreach (var userId in userIds)
            {
                existingTargetUserIds.Add(userId);
            }
        }

        private async Task TryPopulateRedisAsync(
            PhotoProfile profile,
            User user,
            CancellationToken cancellationToken)
        {
            try
            {
                await PopulateRedisAsync(profile, user, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to populate Redis for migrated profile {ProfileId}; DB copy is kept",
                    profile.Id);
            }
        }

        private async Task PopulateRedisAsync(
            PhotoProfile profile,
            User user,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _redis.HashSetAsync(
                PhotoRedisKeys.ProfileHash(profile.Id),
                new HashEntry[]
                {
                    new("name", UserPublicDisplayName.Resolve(user)),
                    new("rating", profile.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("rating_count", profile.RatingCount),
                    new("gender_photo", profile.GenderNomination.ToString()),
                    new("age_photo", profile.AgeNomination),
                    new("user_id", profile.UserId.ToString()),
                });

            var cityKey = _normalizeCityNameService.Normalize(profile.CityNomination.Value ?? string.Empty);
            var sortedSetKey = PhotoRedisKeys.RatingSortedSet(cityKey, TargetSeasonId);
            await _redis.SortedSetAddAsync(
                sortedSetKey,
                profile.Id.ToString(),
                PhotoRankingScore.ToSortScore(profile.Rating, profile.RatingCount));
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
