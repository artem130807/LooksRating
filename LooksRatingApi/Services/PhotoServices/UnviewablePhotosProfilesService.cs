using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LooksRatingApi.Services.PhotoServices
{
    public sealed class UnviewablePhotosProfilesService : IUnviewablePhotosProfilesService
    {
        private readonly IDatabase _db;
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly ILogger<UnviewablePhotosProfilesService> _logger;

        public UnviewablePhotosProfilesService(
            IConnectionMultiplexer redis,
            IUserTicketRepository userTicketRepository,
            ILogger<UnviewablePhotosProfilesService> logger)
        {
            _db = redis.GetDatabase();
            _userTicketRepository = userTicketRepository;
            _logger = logger;
        }

        public async Task<Result> AddUnviewablePhotosProfile(
            Guid photoProfileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (photoProfileId == Guid.Empty)
            {
                return Result.Failure(UnviewablePhotosProfilesErrors.PhotoProfileIdIsRequired);
            }

            if (userId == Guid.Empty)
            {
                return Result.Failure(UnviewablePhotosProfilesErrors.UserIdIsRequired);
            }

            try
            {
                await _db.SetAddAsync(
                    PhotoRedisKeys.UnviewableProfilesSet(userId),
                    photoProfileId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to cache unviewable profile {PhotoProfileId} for user {UserId}",
                    photoProfileId,
                    userId);
                return Result.Failure(UnviewablePhotosProfilesErrors.CacheWriteFailed);
            }

            return Result.Success();
        }

        public async Task<Result> RemoveUnviewablePhotosProfile(
            Guid photoProfileId,
            IReadOnlyCollection<Guid> reporterUserIds,
            CancellationToken cancellationToken = default)
        {
            if (photoProfileId == Guid.Empty)
            {
                return Result.Failure(UnviewablePhotosProfilesErrors.PhotoProfileIdIsRequired);
            }

            if (reporterUserIds is null || reporterUserIds.Count == 0)
            {
                return Result.Success();
            }

            var profileValue = (RedisValue)photoProfileId.ToString();
            var distinctReporterIds = reporterUserIds
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray();

            if (distinctReporterIds.Length == 0)
            {
                return Result.Success();
            }

            try
            {
                var batch = _db.CreateBatch();
                var tasks = distinctReporterIds
                    .Select(userId => batch.SetRemoveAsync(
                        PhotoRedisKeys.UnviewableProfilesSet(userId),
                        profileValue))
                    .ToArray();
                batch.Execute();
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to remove unviewable profile {PhotoProfileId} from cache for {ReporterCount} reporters",
                    photoProfileId,
                    distinctReporterIds.Length);
                return Result.Failure(UnviewablePhotosProfilesErrors.CacheWriteFailed);
            }

            return Result.Success();
        }

        public async Task<Result<List<Guid>>> GetUnviewablePhotosProfile(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                return Result.Failure<List<Guid>>(UnviewablePhotosProfilesErrors.UserIdIsRequired);
            }

            var cacheKey = PhotoRedisKeys.UnviewableProfilesSet(userId);

            try
            {
                var cachedMembers = await _db.SetMembersAsync(cacheKey);
                if (cachedMembers.Length > 0)
                {
                    return Result.Success(ParseProfileIds(cachedMembers));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to read unviewable profiles cache for user {UserId}, falling back to database",
                    userId);
            }

            HashSet<Guid> profileIds;
            try
            {
                profileIds = await _userTicketRepository.GetReportedPhotoProfileIdsByReporterAsync(
                    userId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load reported photo profiles for user {UserId}",
                    userId);
                return Result.Failure<List<Guid>>(UnviewablePhotosProfilesErrors.LoadFailed);
            }

            if (profileIds.Count == 0)
            {
                return Result.Success(new List<Guid>());
            }

            await TryHydrateCacheAsync(cacheKey, profileIds);
            return Result.Success(profileIds.ToList());
        }

        private async Task TryHydrateCacheAsync(RedisKey cacheKey, IReadOnlyCollection<Guid> profileIds)
        {
            if (profileIds.Count == 0)
            {
                return;
            }

            try
            {
                var values = profileIds
                    .Select(profileId => (RedisValue)profileId.ToString())
                    .ToArray();
                await _db.SetAddAsync(cacheKey, values);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to hydrate unviewable profiles cache for key {CacheKey}",
                    cacheKey.ToString());
            }
        }

        private static List<Guid> ParseProfileIds(RedisValue[] members)
        {
            var profileIds = new List<Guid>(members.Length);
            foreach (var member in members)
            {
                if (Guid.TryParse(member.ToString(), out var profileId))
                {
                    profileIds.Add(profileId);
                }
            }

            return profileIds;
        }
    }

    public static class UnviewablePhotosProfilesErrors
    {
        public const string UserIdIsRequired = "UserIdIsRequired";
        public const string PhotoProfileIdIsRequired = "PhotoProfileIdIsRequired";
        public const string CacheWriteFailed = "UnviewableProfilesCacheWriteFailed";
        public const string LoadFailed = "UnviewableProfilesLoadFailed";
    }
}
