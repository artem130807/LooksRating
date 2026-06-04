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
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public PhotoTopReadService(
            IDatabase db,
            INormalizeCityNameService normalizeCityNameService,
            IPhotoProfileRepository photoProfileRepository)
        {
            _db = db;
            _normalizeCityNameService = normalizeCityNameService;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<(IReadOnlyList<Guid> ProfileIds, int TotalCount)> GetTopProfileIdsAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            var cityKey = _normalizeCityNameService.Normalize(cityNomination);
            var sortedSetKey = PhotoRedisKeys.RatingSortedSet(cityKey, seasonId);

            if (!seasonIsClosed && await _db.KeyExistsAsync(sortedSetKey))
            {
                var fromCache = await GetTopFromRedisAsync(sortedSetKey, gender, age, skip, take, vipOnly);
                if (vipOnly || fromCache.TotalCount > 0)
                {
                    return fromCache;
                }
            }

            return await GetTopFromDatabaseAsync(
                seasonId,
                seasonIsClosed,
                cityNomination,
                gender,
                age,
                skip,
                take,
                vipOnly,
                cancellationToken);
        }

        private async Task<(IReadOnlyList<Guid> ProfileIds, int TotalCount)> GetTopFromRedisAsync(
            RedisKey sortedSetKey,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly = false)
        {
            var rankedProfiles = new List<RankedPhoto>();
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

                    var ranking = await TryGetRankedPhotoAsync(photoId, gender, age, vipOnly);
                    if (ranking is null)
                    {
                        continue;
                    }

                    rankedProfiles.Add(ranking);
                }

                rankStart += BatchSize;
                if (candidates.Length < BatchSize)
                {
                    break;
                }
            }

            rankedProfiles.Sort((left, right) =>
                PhotoRankingScore.Compare(left.Rating, left.RatingCount, right.Rating, right.RatingCount));

            var totalCount = rankedProfiles.Count;
            var pageIds = rankedProfiles
                .Skip(skip)
                .Take(take)
                .Select(photo => photo.Id)
                .ToList();

            return (pageIds, totalCount);
        }

        private async Task<RankedPhoto?> TryGetRankedPhotoAsync(
            Guid photoId,
            GenderEnum genderEnum,
            int age,
            bool vipOnly = false)
        {
            var photoKey = PhotoRedisKeys.ProfileHash(photoId);
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
                return await TryGetRankedPhotoFromProfileAsync(photoId, genderEnum, age, vipOnly);
            }

            if (!ratingValue.HasValue
                || !decimal.TryParse(ratingValue.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rating))
            {
                return await TryGetRankedPhotoFromProfileAsync(photoId, genderEnum, age, vipOnly);
            }

            if (vipOnly)
            {
                return await TryGetRankedPhotoFromProfileAsync(photoId, genderEnum, age, vipOnly: true);
            }

            var ratingCount = ratingCountValue.HasValue ? (int)ratingCountValue : 0;
            return new RankedPhoto(photoId, rating, ratingCount);
        }

        private async Task<RankedPhoto?> TryGetRankedPhotoFromProfileAsync(
            Guid photoId,
            GenderEnum genderEnum,
            int age,
            bool vipOnly)
        {
            var profile = await _photoProfileRepository.GetByIdAsync(photoId);
            if (profile is null)
            {
                return null;
            }

            if (vipOnly && profile.User.Status != VipStatus.Availlable)
            {
                return null;
            }

            if (!GenderFeedHelper.Matches(genderEnum, profile.GenderNomination))
            {
                return null;
            }

            if (!TopService.MatchesAge(age, profile.AgeNomination))
            {
                return null;
            }

            return new RankedPhoto(photoId, profile.Rating, profile.RatingCount);
        }

        private async Task<(IReadOnlyList<Guid> ProfileIds, int TotalCount)> GetTopFromDatabaseAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly,
            CancellationToken cancellationToken)
        {
            var ids = await _photoProfileRepository.GetTopProfileIdsAsync(
                seasonId,
                seasonIsClosed,
                cityNomination,
                gender,
                age,
                skip,
                take,
                vipOnly,
                cancellationToken);
            var total = await _photoProfileRepository.CountTopProfilesAsync(
                seasonId,
                seasonIsClosed,
                cityNomination,
                gender,
                age,
                vipOnly,
                cancellationToken);

            return (ids, total);
        }

        private sealed record RankedPhoto(Guid Id, decimal Rating, int RatingCount);
    }
}
