using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.DomainEvents;
using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public sealed class PhotoRatingCacheService : IPhotoRatingCacheService
    {
        private readonly IDatabase _db;
        private readonly INormalizeCityNameService _normalizeCityNameService;

        public PhotoRatingCacheService(
            IConnectionMultiplexer redis,
            INormalizeCityNameService normalizeCityNameService)
        {
            _db = redis.GetDatabase();
            _normalizeCityNameService = normalizeCityNameService;
        }

        public async Task SyncPhotoRatingAsync(PhotoRatedEvent photoRated, CancellationToken cancellationToken = default)
        {
            if (photoRated.SeasonId == Guid.Empty || string.IsNullOrWhiteSpace(photoRated.City))
            {
                return;
            }

            var cityKey = _normalizeCityNameService.Normalize(photoRated.City);
            var sortedSetKey = PhotoRedisKeys.RatingSortedSet(cityKey, photoRated.SeasonId);
            var sortScore = PhotoRankingScore.ToSortScore(photoRated.Rating, photoRated.RatingCount);

            await _db.SortedSetAddAsync(
                sortedSetKey,
                photoRated.AggregateId.ToString(),
                sortScore);

            await _db.HashSetAsync(
                PhotoRedisKeys.ProfileHash(photoRated.AggregateId),
                new HashEntry[]
                {
                    new("rating", photoRated.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("rating_count", photoRated.RatingCount)
                });
        }

        public async Task MarkProfileAsRatedAsync(
            Guid reviewerUserId,
            Guid seasonId,
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            var added = await _db.SetAddAsync(
                PhotoRedisKeys.UserRatedSet(reviewerUserId, seasonId),
                photoProfileId.ToString());

            if (added)
            {
                await _db.StringIncrementAsync(
                    PhotoRedisKeys.FeedRatingCounter(reviewerUserId, seasonId));
            }
        }

        public async Task ResetProfileRatingAsync(
            Guid profileId,
            Guid seasonId,
            string previousCity,
            string newCity,
            CancellationToken cancellationToken = default)
        {
            if (seasonId == Guid.Empty)
            {
                return;
            }

            var previousCityKey = _normalizeCityNameService.Normalize(previousCity);
            var newCityKey = _normalizeCityNameService.Normalize(newCity);
            var profileKey = profileId.ToString();

            if (!string.IsNullOrWhiteSpace(previousCity))
            {
                await _db.SortedSetRemoveAsync(
                    PhotoRedisKeys.RatingSortedSet(previousCityKey, seasonId),
                    profileKey);
            }

            if (!string.IsNullOrWhiteSpace(newCity))
            {
                await _db.SortedSetAddAsync(
                    PhotoRedisKeys.RatingSortedSet(newCityKey, seasonId),
                    profileKey,
                    PhotoRankingScore.ToSortScore(0m, 0));
            }

            await _db.HashSetAsync(
                PhotoRedisKeys.ProfileHash(profileId),
                new HashEntry[]
                {
                    new("rating", 0m.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("rating_count", 0)
                });
        }

        public async Task ClearRatedMarkersForProfileAsync(
            Guid photoProfileId,
            Guid seasonId,
            IReadOnlyCollection<Guid> reviewerUserIds,
            CancellationToken cancellationToken = default)
        {
            if (seasonId == Guid.Empty || reviewerUserIds.Count == 0)
            {
                return;
            }

            var profileKey = photoProfileId.ToString();
            var tasks = new List<Task>(reviewerUserIds.Count);

            foreach (var reviewerUserId in reviewerUserIds)
            {
                tasks.Add(_db.SetRemoveAsync(
                    PhotoRedisKeys.UserRatedSet(reviewerUserId, seasonId),
                    profileKey));
            }

            await Task.WhenAll(tasks);
        }

        public async Task SyncProfileDisplayNameAsync(
            Guid profileId,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            if (profileId == Guid.Empty || string.IsNullOrWhiteSpace(displayName))
            {
                return;
            }

            await _db.HashSetAsync(
                PhotoRedisKeys.ProfileHash(profileId),
                new HashEntry[]
                {
                    new("name", displayName),
                });
        }
    }
}
