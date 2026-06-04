using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Enums;
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
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public PhotoRecommendationService(
            IConnectionMultiplexer redis,
            INormalizeCityNameService normalizeCityNameService,
            ISeasonRepository seasonRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _db = redis.GetDatabase();
            _normalizeCityNameService = normalizeCityNameService;
            _seasonRepository = seasonRepository;
            _photoProfileRepository = photoProfileRepository;
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
            double? lastScore = null)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return new List<Guid>();
            }

            var cityKey = _normalizeCityNameService.Normalize(city);
            var context = new FeedSearchContext(
                season.Id,
                reviewerUserId,
                city,
                cityKey,
                genderEnum,
                age,
                PhotoRedisKeys.RatingSortedSet(cityKey, season.Id),
                PhotoRedisKeys.UserRatedSet(reviewerUserId, season.Id),
                PhotoRedisKeys.CycleAnchor(reviewerUserId, season.Id),
                VipOnly: false);

            await EnsureCycleAnchorAsync(context.CycleAnchorKey);

            var ratedProfileIds = await LoadRatedPhotoIdsAsync(context.RatedSetKey);
            var completedRatings = await GetFeedRatingCounterAsync(reviewerUserId, season.Id);

            if (IsVipFeedTurn(completedRatings))
            {
                var vipContext = context with { VipOnly = true };
                var vipProfileId = await TryGetNextProfileAsync(vipContext, ratedProfileIds);
                if (vipProfileId.HasValue)
                {
                    return [vipProfileId.Value];
                }
            }

            var profileId = await TryGetNextProfileAsync(context, ratedProfileIds);
            if (profileId.HasValue)
            {
                return [profileId.Value];
            }

            return new List<Guid>();
        }

        private async Task<Guid?> TryGetNextProfileAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds)
        {
            var profileId = await TryGetByRandomOrderAsync(context, ratedProfileIds);
            if (profileId.HasValue)
            {
                return profileId;
            }

            if (!await IsCycleCompleteAsync(context, ratedProfileIds))
            {
                return null;
            }

            return await RestartCycleAndGetNextAsync(context);
        }

        private static bool IsVipFeedTurn(int completedRatings) =>
            (completedRatings + 1) % VipFeedInterval == 0;

        private async Task<Guid?> RestartCycleAndGetNextAsync(FeedSearchContext context)
        {
            var previousAnchor = await GetCycleAnchorAsync(context.CycleAnchorKey);

            await _db.KeyDeleteAsync(context.RatedSetKey);
            await SetCycleAnchorAsync(context.CycleAnchorKey, DateTime.UtcNow);

            var profileId = await TryGetByRandomOrderAsync(
                context,
                new HashSet<Guid>(),
                createdAfter: previousAnchor);
            if (profileId.HasValue)
            {
                return profileId;
            }

            return await TryGetByRandomOrderAsync(context, new HashSet<Guid>());
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
            DateTime? createdAfter = null)
        {
            if (createdAfter.HasValue)
            {
                return await TryGetFromDatabaseRandomAsync(context, ratedProfileIds, createdAfter.Value);
            }

            var profileId = await TryGetFromRandomSortedSetAsync(context, ratedProfileIds);
            if (profileId.HasValue)
            {
                return profileId;
            }

            return await TryGetFromDatabaseRandomAsync(context, ratedProfileIds);
        }

        private async Task<Guid?> TryGetFromRandomSortedSetAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds)
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
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            bool vipOnly)
        {
            var eligibleProfileIds = new List<Guid>(profileIds.Count);
            foreach (var profileId in profileIds)
            {
                if (ratedProfileIds.Contains(profileId))
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
                if (!GenderFeedHelper.Matches(genderEnum, gender.ToString()))
                {
                    return false;
                }

                var photoAge = (int)ageValue;
                if (!TopService.MatchesAge(age, photoAge))
                {
                    return false;
                }

                if (ownerValue.HasValue && Guid.TryParse(ownerValue.ToString(), out var ownerId))
                {
                    if (ownerId == reviewerUserId)
                    {
                        return false;
                    }

                    if (!vipOnly)
                    {
                        return true;
                    }
                }
            }

            var photoProfile = await _photoProfileRepository.GetByIdAsync(profileId);
            if (photoProfile is null || photoProfile.UserId == reviewerUserId)
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
