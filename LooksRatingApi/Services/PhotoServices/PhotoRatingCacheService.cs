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
                PhotoRedisKeys.PhotoHash(photoRated.AggregateId),
                new HashEntry[]
                {
                    new("rating", photoRated.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("rating_count", photoRated.RatingCount)
                });
        }

        public Task MarkPhotoAsRatedAsync(
            Guid reviewerUserId,
            Guid photoUserId,
            CancellationToken cancellationToken = default)
        {
            return _db.SetAddAsync(
                PhotoRedisKeys.UserRatedSet(reviewerUserId),
                photoUserId.ToString());
        }
    }
}
