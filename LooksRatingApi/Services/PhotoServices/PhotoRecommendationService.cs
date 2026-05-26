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

        private readonly IDatabase _db;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoUserRepository _photoUserRepository;

        public PhotoRecommendationService(
            IConnectionMultiplexer redis,
            INormalizeCityNameService normalizeCityNameService,
            ISeasonRepository seasonRepository,
            IPhotoUserRepository photoUserRepository)
        {
            _db = redis.GetDatabase();
            _normalizeCityNameService = normalizeCityNameService;
            _seasonRepository = seasonRepository;
            _photoUserRepository = photoUserRepository;
        }

        public async Task<Guid?> GetNextUnratedPhotoIdAsync(
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            string city,
            double? lastScore = null)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return null;
            }

            var cityKey = _normalizeCityNameService.Normalize(city);
            var context = new FeedSearchContext(
                season.Id,
                reviewerUserId,
                cityKey,
                genderEnum,
                age,
                PhotoRedisKeys.RatingSortedSet(cityKey, season.Id),
                PhotoRedisKeys.UserRatedSet(reviewerUserId),
                PhotoRedisKeys.CycleAnchor(reviewerUserId));

            await EnsureCycleAnchorAsync(context.CycleAnchorKey);

            var ratedPhotoIds = await LoadRatedPhotoIdsAsync(context.RatedSetKey);

            var photoId = await TryGetByRatingOrderAsync(context, ratedPhotoIds, lastScore);
            if (photoId.HasValue)
            {
                return photoId;
            }

            if (!await IsCycleCompleteAsync(context, ratedPhotoIds))
            {
                return null;
            }

            return await RestartCycleAndGetNextAsync(context);
        }

        private async Task<Guid?> RestartCycleAndGetNextAsync(FeedSearchContext context)
        {
            var previousAnchor = await GetCycleAnchorAsync(context.CycleAnchorKey);

            await _db.KeyDeleteAsync(context.RatedSetKey);
            await SetCycleAnchorAsync(context.CycleAnchorKey, DateTime.UtcNow);

            var photoId = await TryGetByCreatedAtOrderAsync(context, previousAnchor, new HashSet<Guid>());
            if (photoId.HasValue)
            {
                return photoId;
            }

            return await TryGetByRatingOrderAsync(context, new HashSet<Guid>(), lastScore: null);
        }

        private async Task<Guid?> TryGetByRatingOrderAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedPhotoIds,
            double? lastScore)
        {
            var photoId = await TryGetFromRatingSortedSetAsync(context, ratedPhotoIds, lastScore);
            if (photoId.HasValue)
            {
                return photoId;
            }

            return await TryGetFromDatabaseByRatingAsync(context, ratedPhotoIds);
        }

        private async Task<Guid?> TryGetByCreatedAtOrderAsync(
            FeedSearchContext context,
            DateTime createdAfter,
            HashSet<Guid> ratedPhotoIds)
        {
            var skip = 0;
            while (true)
            {
                var candidates = await _photoUserRepository.GetNewFeedCandidateIdsAsync(
                    context.SeasonId,
                    context.ReviewerUserId,
                    context.CityKey,
                    context.Gender,
                    context.Age,
                    createdAfter,
                    skip,
                    BatchSize);

                if (candidates.Count == 0)
                {
                    return null;
                }

                var photoId = await SelectFirstEligiblePhotoAsync(
                    candidates,
                    ratedPhotoIds,
                    context.ReviewerUserId,
                    context.Gender,
                    context.Age);
                if (photoId.HasValue)
                {
                    return photoId;
                }

                skip += BatchSize;
                if (candidates.Count < BatchSize)
                {
                    return null;
                }
            }
        }

        private async Task<Guid?> TryGetFromRatingSortedSetAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedPhotoIds,
            double? lastScore)
        {
            var maxScore = lastScore ?? double.PositiveInfinity;

            while (true)
            {
                var candidates = await _db.SortedSetRangeByScoreAsync(
                    context.SortedSetKey,
                    start: 0,
                    stop: maxScore,
                    exclude: maxScore == double.PositiveInfinity ? Exclude.None : Exclude.Stop,
                    order: Order.Descending,
                    take: BatchSize);

                if (candidates.Length == 0)
                {
                    return null;
                }

                var photoIds = ParsePhotoIds(candidates);
                var photoId = await SelectFirstEligiblePhotoAsync(
                    photoIds,
                    ratedPhotoIds,
                    context.ReviewerUserId,
                    context.Gender,
                    context.Age);
                if (photoId.HasValue)
                {
                    return photoId;
                }

                var lastScoreInBatch = await _db.SortedSetScoreAsync(context.SortedSetKey, candidates[^1]);
                if (!lastScoreInBatch.HasValue || candidates.Length < BatchSize)
                {
                    return null;
                }

                maxScore = lastScoreInBatch.Value;
            }
        }

        private async Task<Guid?> TryGetFromDatabaseByRatingAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedPhotoIds)
        {
            var skip = 0;
            while (true)
            {
                var candidates = await _photoUserRepository.GetFeedCandidateIdsAsync(
                    context.SeasonId,
                    context.ReviewerUserId,
                    context.CityKey,
                    context.Gender,
                    context.Age,
                    skip,
                    BatchSize);

                if (candidates.Count == 0)
                {
                    return null;
                }

                var photoId = await SelectFirstEligiblePhotoAsync(
                    candidates,
                    ratedPhotoIds,
                    context.ReviewerUserId,
                    context.Gender,
                    context.Age);
                if (photoId.HasValue)
                {
                    return photoId;
                }

                skip += BatchSize;
                if (candidates.Count < BatchSize)
                {
                    return null;
                }
            }
        }

        private async Task<Guid?> SelectFirstEligiblePhotoAsync(
            IReadOnlyList<Guid> photoIds,
            HashSet<Guid> ratedPhotoIds,
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age)
        {
            foreach (var photoId in photoIds)
            {
                if (ratedPhotoIds.Contains(photoId))
                {
                    continue;
                }

                if (!await MatchesFeedCriteriaAsync(photoId, reviewerUserId, genderEnum, age))
                {
                    continue;
                }

                return photoId;
            }

            return null;
        }

        private async Task<bool> IsCycleCompleteAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedPhotoIds)
        {
            if (ratedPhotoIds.Count == 0)
            {
                return false;
            }

            var feedCount = await _photoUserRepository.CountFeedPhotosAsync(
                context.SeasonId,
                context.ReviewerUserId,
                context.CityKey,
                context.Gender,
                context.Age);

            return feedCount > 0 && ratedPhotoIds.Count >= feedCount;
        }

        private async Task<bool> MatchesFeedCriteriaAsync(
            Guid photoId,
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age)
        {
            var photoKey = PhotoRedisKeys.PhotoHash(photoId);
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
                    return ownerId != reviewerUserId;
                }
            }

            var photoUser = await _photoUserRepository.GePhotoUserById(photoId);
            if (photoUser is null || photoUser.UserId == reviewerUserId)
            {
                return false;
            }

            if (!GenderFeedHelper.Matches(genderEnum, photoUser.GenderNomination))
            {
                return false;
            }

            return TopService.MatchesAge(age, photoUser.AgeNomination);
        }

        private static List<Guid> ParsePhotoIds(RedisValue[] candidates)
        {
            var photoIds = new List<Guid>(candidates.Length);
            foreach (var candidate in candidates)
            {
                if (Guid.TryParse(candidate, out var photoId))
                {
                    photoIds.Add(photoId);
                }
            }

            return photoIds;
        }

        private async Task<HashSet<Guid>> LoadRatedPhotoIdsAsync(RedisKey ratedSetKey)
        {
            var members = await _db.SetMembersAsync(ratedSetKey);
            var ratedPhotoIds = new HashSet<Guid>(members.Length);
            foreach (var member in members)
            {
                if (Guid.TryParse(member.ToString(), out var photoId))
                {
                    ratedPhotoIds.Add(photoId);
                }
            }

            return ratedPhotoIds;
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
            string CityKey,
            GenderEnum Gender,
            int Age,
            RedisKey SortedSetKey,
            RedisKey RatedSetKey,
            RedisKey CycleAnchorKey);
    }
}
