using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public class PhotoRecommendationService : IPhotoRecommendationService
    {
        private const int BatchSize = 50;
        private const int VipFeedInterval = 5;
        private const int MaxRandomBatchAttempts = 20;

        private readonly IDatabase _db;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly ICityService _cityService;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IUnviewablePhotosProfilesService _unviewablePhotosProfilesService;
        private readonly ILogger<PhotoRecommendationService> _logger;

        public PhotoRecommendationService(
            IConnectionMultiplexer redis,
            INormalizeCityNameService normalizeCityNameService,
            ICityService cityService,
            ISeasonRepository seasonRepository,
            IPhotoProfileRepository photoProfileRepository,
            IUnviewablePhotosProfilesService unviewablePhotosProfilesService,
            ILogger<PhotoRecommendationService> logger)
        {
            _db = redis.GetDatabase();
            _normalizeCityNameService = normalizeCityNameService;
            _cityService = cityService;
            _seasonRepository = seasonRepository;
            _photoProfileRepository = photoProfileRepository;
            _unviewablePhotosProfilesService = unviewablePhotosProfilesService;
            _logger = logger;
        }

        public async Task<Guid?> GetNextUnratedProfileIdAsync(
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            string city,
            double? lastScore = null)
        {
            var ids = await GetNextUnratedProfileIdsAsync(reviewerUserId, genderEnum, age, city, lastScore);
            return ids.Count > 0 ? ids[0] : null;
        }

        public async Task<List<Guid>> GetNextUnratedProfileIdsAsync(
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            string city,
            double? lastScore = null,
            IReadOnlyCollection<Guid>? skipProfileIds = null)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                _logger.LogWarning(
                    "Feed empty: no active season for reviewer {ReviewerUserId}",
                    reviewerUserId);
                return new List<Guid>();
            }

            var feedCity = ResolveFeedCity(city);
            if (string.IsNullOrWhiteSpace(feedCity))
            {
                _logger.LogWarning(
                    "Feed empty: unresolved city '{City}' for reviewer {ReviewerUserId}",
                    city,
                    reviewerUserId);
                return new List<Guid>();
            }

            var cityKey = _normalizeCityNameService.Normalize(feedCity);
            var context = new FeedSearchContext(
                season.Id,
                reviewerUserId,
                feedCity,
                cityKey,
                genderEnum,
                age,
                PhotoRedisKeys.RatingSortedSet(cityKey, season.Id),
                PhotoRedisKeys.UserRatedSet(reviewerUserId, season.Id),
                PhotoRedisKeys.CycleAnchor(reviewerUserId, season.Id),
                VipOnly: false);

            await EnsureCycleAnchorAsync(context.CycleAnchorKey);

            var ratedProfileIds = await LoadRatedPhotoIdsAsync(context.RatedSetKey);
            var unviewableProfileIds = await LoadUnviewableProfileIdsAsync(reviewerUserId);
            var skipIds = skipProfileIds?.ToHashSet() ?? new HashSet<Guid>();
            var completedRatings = await GetFeedRatingCounterAsync(reviewerUserId, season.Id);

            if (IsVipFeedTurn(completedRatings))
            {
                var vipContext = context with { VipOnly = true };
                var vipProfileId = await TryGetNextProfileAsync(
                    vipContext,
                    ratedProfileIds,
                    unviewableProfileIds,
                    skipIds);
                if (vipProfileId.HasValue)
                {
                    return [vipProfileId.Value];
                }
            }

            var profileId = await TryGetNextProfileAsync(
                context,
                ratedProfileIds,
                unviewableProfileIds,
                skipIds);
            if (profileId.HasValue)
            {
                return [profileId.Value];
            }

            var feedCount = await _photoProfileRepository.CountFeedProfilesAsync(
                season.Id,
                reviewerUserId,
                feedCity,
                genderEnum,
                age);

            _logger.LogInformation(
                "Feed empty for reviewer {ReviewerUserId}: city={City}, gender={Gender}, age={Age}, season={SeasonId}, rated={RatedCount}, unviewable={UnviewableCount}, skipped={SkippedCount}, feedCandidates={FeedCount}",
                reviewerUserId,
                feedCity,
                genderEnum,
                age,
                season.Id,
                ratedProfileIds.Count,
                unviewableProfileIds.Count,
                skipIds.Count,
                feedCount);

            return new List<Guid>();
        }

        private string ResolveFeedCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return string.Empty;
            }

            if (_cityService.TryResolveCanonicalCity(city, out var canonicalCity))
            {
                return canonicalCity;
            }

            return city.Trim().ToLowerInvariant();
        }

        private async Task<Guid?> TryGetNextProfileAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            var profileId = await TryGetByRandomOrderAsync(
                context,
                ratedProfileIds,
                unviewableProfileIds,
                skipProfileIds);
            if (profileId.HasValue)
            {
                return profileId;
            }

            if (await IsCycleCompleteAsync(context, ratedProfileIds))
            {
                return await RestartCycleAndGetNextAsync(context, unviewableProfileIds, skipProfileIds);
            }

            if (ratedProfileIds.Count > 0)
            {
                _logger.LogInformation(
                    "Feed stuck for reviewer {ReviewerUserId}: rated={RatedCount}, restarting cycle early",
                    context.ReviewerUserId,
                    ratedProfileIds.Count);
                return await RestartCycleAndGetNextAsync(context, unviewableProfileIds, skipProfileIds);
            }

            return null;
        }

        private static bool IsVipFeedTurn(int completedRatings) =>
            (completedRatings + 1) % VipFeedInterval == 0;

        private async Task<Guid?> RestartCycleAndGetNextAsync(
            FeedSearchContext context,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            var previousAnchor = await GetCycleAnchorAsync(context.CycleAnchorKey);

            await _db.KeyDeleteAsync(context.RatedSetKey);
            await SetCycleAnchorAsync(context.CycleAnchorKey, DateTime.UtcNow);

            var profileId = await TryGetByRandomOrderAsync(
                context,
                new HashSet<Guid>(),
                unviewableProfileIds,
                skipProfileIds,
                createdAfter: previousAnchor);
            if (profileId.HasValue)
            {
                return profileId;
            }

            return await TryGetByRandomOrderAsync(
                context,
                new HashSet<Guid>(),
                unviewableProfileIds,
                skipProfileIds);
        }

        private async Task<int> GetFeedRatingCounterAsync(Guid reviewerUserId, Guid seasonId)
        {
            var value = await _db.StringGetAsync(PhotoRedisKeys.FeedRatingCounter(reviewerUserId, seasonId));
            if (!value.HasValue || !long.TryParse(value.ToString(), out var count) || count < 0)
            {
                return 0;
            }

            return count > int.MaxValue ? int.MaxValue : (int)count;
        }

        private async Task<Guid?> TryGetByRandomOrderAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds,
            DateTime? createdAfter = null)
        {
            if (createdAfter.HasValue)
            {
                return await TryGetFromDatabaseRandomAsync(
                    context,
                    ratedProfileIds,
                    unviewableProfileIds,
                    skipProfileIds,
                    createdAfter.Value);
            }

            var profileId = await TryGetFromRandomSortedSetAsync(
                context,
                ratedProfileIds,
                unviewableProfileIds,
                skipProfileIds);
            if (profileId.HasValue)
            {
                return profileId;
            }

            return await TryGetFromDatabaseRandomAsync(
                context,
                ratedProfileIds,
                unviewableProfileIds,
                skipProfileIds);
        }

        private async Task<Guid?> TryGetFromRandomSortedSetAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            if (!await _db.KeyExistsAsync(context.SortedSetKey))
            {
                return null;
            }

            for (var attempt = 0; attempt < MaxRandomBatchAttempts; attempt++)
            {
                var members = await _db.SortedSetRandomMembersAsync(context.SortedSetKey, BatchSize);
                if (members.Length == 0)
                {
                    return null;
                }

                var profileId = await SelectRandomEligiblePhotoAsync(
                    ParseProfileIds(members),
                    ratedProfileIds,
                    unviewableProfileIds,
                    skipProfileIds,
                    context.ReviewerUserId,
                    context.Gender,
                    context.Age,
                    context.VipOnly);
                if (profileId.HasValue)
                {
                    return profileId;
                }

                if (members.Length < BatchSize)
                {
                    return null;
                }
            }

            return null;
        }

        private async Task<Guid?> TryGetFromDatabaseRandomAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds,
            DateTime? createdAfter = null)
        {
            for (var attempt = 0; attempt < MaxRandomBatchAttempts; attempt++)
            {
                var candidates = createdAfter.HasValue
                    ? await _photoProfileRepository.GetRandomNewFeedCandidateProfileIdsAsync(
                        context.SeasonId,
                        context.ReviewerUserId,
                        context.CityNomination,
                        context.Gender,
                        context.Age,
                        createdAfter.Value,
                        BatchSize,
                        context.VipOnly)
                    : await _photoProfileRepository.GetRandomFeedCandidateProfileIdsAsync(
                        context.SeasonId,
                        context.ReviewerUserId,
                        context.CityNomination,
                        context.Gender,
                        context.Age,
                        BatchSize,
                        context.VipOnly);

                if (candidates.Count == 0)
                {
                    return null;
                }

                var profileId = await SelectRandomEligiblePhotoAsync(
                    candidates,
                    ratedProfileIds,
                    unviewableProfileIds,
                    skipProfileIds,
                    context.ReviewerUserId,
                    context.Gender,
                    context.Age,
                    context.VipOnly);
                if (profileId.HasValue)
                {
                    return profileId;
                }

                if (candidates.Count < BatchSize)
                {
                    return null;
                }
            }

            return null;
        }

        private async Task<Guid?> SelectRandomEligiblePhotoAsync(
            IReadOnlyList<Guid> profileIds,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds,
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            bool vipOnly)
        {
            var eligibleProfileIds = new List<Guid>(profileIds.Count);
            foreach (var profileId in profileIds)
            {
                if (IsExcludedProfile(profileId, ratedProfileIds, unviewableProfileIds, skipProfileIds))
                {
                    continue;
                }

                if (!await MatchesFeedCriteriaAsync(profileId, reviewerUserId, genderEnum, age, vipOnly))
                {
                    continue;
                }

                eligibleProfileIds.Add(profileId);
            }

            if (eligibleProfileIds.Count == 0)
            {
                return null;
            }

            var randomIndex = Random.Shared.Next(eligibleProfileIds.Count);
            return eligibleProfileIds[randomIndex];
        }

        private static bool IsExcludedProfile(
            Guid profileId,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds) =>
            ratedProfileIds.Contains(profileId)
            || unviewableProfileIds.Contains(profileId)
            || skipProfileIds.Contains(profileId);

        private async Task<bool> IsCycleCompleteAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds)
        {
            if (ratedProfileIds.Count == 0)
            {
                return false;
            }

            var feedCount = await _photoProfileRepository.CountFeedProfilesAsync(
                context.SeasonId,
                context.ReviewerUserId,
                context.CityNomination,
                context.Gender,
                context.Age);

            return feedCount > 0 && ratedProfileIds.Count >= feedCount;
        }

        private async Task<bool> MatchesFeedCriteriaAsync(
            Guid profileId,
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            bool vipOnly)
        {
            var photoKey = PhotoRedisKeys.ProfileHash(profileId);
            var hashValues = await _db.HashGetAsync(
                photoKey,
                new RedisValue[] { "gender_photo", "age_photo", "user_id" });

            var gender = hashValues[0];
            var ageValue = hashValues[1];
            var ownerValue = hashValues[2];

            if (!gender.IsNullOrEmpty && !ageValue.IsNullOrEmpty)
            {
                var redisGenderMatches = GenderFeedHelper.Matches(genderEnum, gender.ToString());
                var photoAge = (int)ageValue;
                var redisAgeMatches = TopService.MatchesAge(age, photoAge);

                if (redisGenderMatches && redisAgeMatches)
                {
                    if (ownerValue.HasValue && Guid.TryParse(ownerValue.ToString(), out var ownerId)
                        && ownerId == reviewerUserId)
                    {
                        return false;
                    }
                }
            }

            var photoProfile = await _photoProfileRepository.GetByIdAsync(profileId);
            if (photoProfile is null || photoProfile.UserId == reviewerUserId)
            {
                return false;
            }

            if (!HasDisplayablePhotos(photoProfile))
            {
                return false;
            }

            if (vipOnly && photoProfile.User.Status != VipStatus.Availlable)
            {
                return false;
            }

            if (!GenderFeedHelper.Matches(genderEnum, photoProfile.GenderNomination))
            {
                return false;
            }

            return TopService.MatchesAge(age, photoProfile.AgeNomination);
        }

        private static bool HasDisplayablePhotos(PhotoProfile photoProfile) =>
            photoProfile.Photos.Any(photo => !string.IsNullOrWhiteSpace(photo.TelegramFileId));

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

        private async Task<HashSet<Guid>> LoadRatedPhotoIdsAsync(RedisKey ratedSetKey)
        {
            var members = await _db.SetMembersAsync(ratedSetKey);
            var ratedProfileIds = new HashSet<Guid>(members.Length);
            foreach (var member in members)
            {
                if (Guid.TryParse(member.ToString(), out var profileId))
                {
                    ratedProfileIds.Add(profileId);
                }
            }

            return ratedProfileIds;
        }

        private async Task<HashSet<Guid>> LoadUnviewableProfileIdsAsync(Guid reviewerUserId)
        {
            var result = await _unviewablePhotosProfilesService.GetUnviewablePhotosProfile(reviewerUserId);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to load unviewable photo profiles for reviewer {ReviewerUserId}: {Error}",
                    reviewerUserId,
                    result.Error);
                return new HashSet<Guid>();
            }

            return result.Value.ToHashSet();
        }

        private async Task EnsureCycleAnchorAsync(RedisKey cycleAnchorKey)
        {
            if (!await _db.KeyExistsAsync(cycleAnchorKey))
            {
                await SetCycleAnchorAsync(cycleAnchorKey, DateTime.UtcNow);
            }
        }

        private async Task<DateTime> GetCycleAnchorAsync(RedisKey cycleAnchorKey)
        {
            var value = await _db.StringGetAsync(cycleAnchorKey);
            if (!value.HasValue || !long.TryParse(value.ToString(), out var ticks))
            {
                return DateTime.UtcNow;
            }

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private Task SetCycleAnchorAsync(RedisKey cycleAnchorKey, DateTime utcNow) =>
            _db.StringSetAsync(cycleAnchorKey, utcNow.Ticks.ToString());

        private sealed record FeedSearchContext(
            Guid SeasonId,
            Guid ReviewerUserId,
            string CityNomination,
            string CityKey,
            GenderEnum Gender,
            int Age,
            RedisKey SortedSetKey,
            RedisKey RatedSetKey,
            RedisKey CycleAnchorKey,
            bool VipOnly);
    }
}
