using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Enums;
using StackExchange.Redis;
using System.Globalization;

namespace LooksRatingApi.Services
{
    public sealed class PhotoTopReadService : IPhotoTopReadService
    {
        private const int BatchSize = 100;

        private readonly IDatabase _db;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly IPhotoUserRepository _photoUserRepository;

        public PhotoTopReadService(
            IDatabase db,
            INormalizeCityNameService normalizeCityNameService,
            IPhotoUserRepository photoUserRepository)
        {
            _db = db;
            _normalizeCityNameService = normalizeCityNameService;
            _photoUserRepository = photoUserRepository;
        }

        public async Task<(IReadOnlyList<Guid> PhotoIds, int TotalCount)> GetTopPhotoIdsAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string normalizedCity,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var cityKey = _normalizeCityNameService.Normalize(normalizedCity);
            var sortedSetKey = PhotoRedisKeys.RatingSortedSet(cityKey, seasonId);

            if (!seasonIsClosed && await _db.KeyExistsAsync(sortedSetKey))
            {
                var fromCache = await GetTopFromRedisAsync(sortedSetKey, gender, age, skip, take);
                if (fromCache.TotalCount > 0)
                {
                    return fromCache;
                }
            }

            return await GetTopFromDatabaseAsync(
                seasonId,
                seasonIsClosed,
                cityKey,
                gender,
                age,
                skip,
                take,
                cancellationToken);
        }

        private async Task<(IReadOnlyList<Guid> PhotoIds, int TotalCount)> GetTopFromRedisAsync(
            RedisKey sortedSetKey,
            GenderEnum gender,
            int age,
            int skip,
            int take)
        {
            var rankedPhotos = new List<RankedPhoto>();
            long rankStart = 0;

            while (true)
            {
                var candidates = await _db.SortedSetRangeByRankAsync(
                    sortedSetKey,
                    rankStart,
                    rankStart + BatchSize - 1,
                    Order.Descending);

                if (candidates.Length == 0)
                {
                    break;
                }

                foreach (var candidate in candidates)
                {
                    if (!Guid.TryParse(candidate, out var photoId))
                    {
                        continue;
                    }

                    var ranking = await TryGetRankedPhotoAsync(photoId, gender, age);
                    if (ranking is null)
                    {
                        continue;
                    }

                    rankedPhotos.Add(ranking);
                }

                rankStart += BatchSize;
                if (candidates.Length < BatchSize)
                {
                    break;
                }
            }

            rankedPhotos.Sort((left, right) =>
                PhotoRankingScore.Compare(left.Rating, left.RatingCount, right.Rating, right.RatingCount));

            var totalCount = rankedPhotos.Count;
            var pageIds = rankedPhotos
                .Skip(skip)
                .Take(take)
                .Select(photo => photo.Id)
                .ToList();

            return (pageIds, totalCount);
        }

        private async Task<RankedPhoto?> TryGetRankedPhotoAsync(
            Guid photoId,
            GenderEnum genderEnum,
            int age)
        {
            var photoKey = PhotoRedisKeys.PhotoHash(photoId);
            var hashValues = await _db.HashGetAsync(
                photoKey,
                new RedisValue[] { "gender_photo", "age_photo", "rating", "rating_count" });

            var gender = hashValues[0];
            var ageValue = hashValues[1];
            var ratingValue = hashValues[2];
            var ratingCountValue = hashValues[3];

            if (!gender.IsNullOrEmpty && !ageValue.IsNullOrEmpty)
            {
                if (!GenderFeedHelper.Matches(genderEnum, gender.ToString()))
                {
                    return null;
                }

                var photoAge = (int)ageValue;
                if (!TopService.MatchesAge(age, photoAge))
                {
                    return null;
                }
            }
            else
            {
                var photoUser = await _photoUserRepository.GePhotoUserById(photoId);
                if (photoUser is null)
                {
                    return null;
                }

                if (!GenderFeedHelper.Matches(genderEnum, photoUser.GenderNomination))
                {
                    return null;
                }

                if (!TopService.MatchesAge(age, photoUser.AgeNomination))
                {
                    return null;
                }

                return new RankedPhoto(photoId, photoUser.Rating, photoUser.RatingCount);
            }

            if (!ratingValue.HasValue
                || !decimal.TryParse(ratingValue.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rating))
            {
                var photoUser = await _photoUserRepository.GePhotoUserById(photoId);
                if (photoUser is null)
                {
                    return null;
                }

                return new RankedPhoto(photoId, photoUser.Rating, photoUser.RatingCount);
            }

            var ratingCount = ratingCountValue.HasValue ? (int)ratingCountValue : 0;
            return new RankedPhoto(photoId, rating, ratingCount);
        }

        private async Task<(IReadOnlyList<Guid> PhotoIds, int TotalCount)> GetTopFromDatabaseAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityKey,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var (photos, total) = await _photoUserRepository.GetTopPhotosPagedAsync(
                seasonId,
                seasonIsClosed,
                cityKey,
                gender,
                age,
                skip,
                take,
                cancellationToken);

            return (photos.Select(p => p.Id).ToList(), total);
        }

        private sealed record RankedPhoto(Guid Id, decimal Rating, int RatingCount);
    }
}
