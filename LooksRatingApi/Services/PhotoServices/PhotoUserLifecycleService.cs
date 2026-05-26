using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public interface IPhotoUserLifecycleService
    {
        Task<PhotoUser> CreateAsync(
            User user,
            string telegramFileId,
            Season season,
            int ageNomination,
            GenderEnum genderNomination,
            CityVo cityNomination,
            CancellationToken cancellationToken);
        Task RemoveAsync(PhotoUser photoUser, Season season, CancellationToken cancellationToken);
    }

    public sealed class PhotoUserLifecycleService : IPhotoUserLifecycleService
    {
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly IDatabase _db;

        public PhotoUserLifecycleService(
            IPhotoUserRepository photoUserRepository,
            INormalizeCityNameService normalizeCityNameService,
            IConnectionMultiplexer redis)
        {
            _photoUserRepository = photoUserRepository;
            _normalizeCityNameService = normalizeCityNameService;
            _db = redis.GetDatabase();
        }

        public async Task<PhotoUser> CreateAsync(
            User user,
            string telegramFileId,
            Season season,
            int ageNomination,
            GenderEnum genderNomination,
            CityVo cityNomination,
            CancellationToken cancellationToken)
        {
            var photoUser = new PhotoUser
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TelegramFileId = telegramFileId,
                Rank = RankEnum.Terrible,
                AgeNomination = ageNomination,
                GenderNomination = genderNomination,
                CityNomination = cityNomination,
                SeasonId = season.Id,
                Status = StatusEnum.Active,
                Rating = 0m,
                RatingCount = 0,
            };

            var photoKey = PhotoRedisKeys.PhotoHash(photoUser.Id);
            await _db.HashSetAsync(photoKey, new HashEntry[]
            {
                new("name", UserPublicDisplayName.Resolve(user)),
                new("rating", photoUser.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("rating_count", 0),
                new("gender_photo", photoUser.GenderNomination.ToString()),
                new("age_photo", photoUser.AgeNomination),
                new("user_id", photoUser.UserId.ToString())
            });

            var cityKey = _normalizeCityNameService.Normalize(photoUser.CityNomination.Value ?? string.Empty);
            var sortedSetKey = PhotoRedisKeys.RatingSortedSet(cityKey, season.Id);
            await _db.SortedSetAddAsync(
                sortedSetKey,
                photoUser.Id.ToString(),
                PhotoRankingScore.ToSortScore(photoUser.Rating, photoUser.RatingCount));

            await _photoUserRepository.Create(photoUser);
            return photoUser;
        }

        public async Task RemoveAsync(PhotoUser photoUser, Season season, CancellationToken cancellationToken)
        {
            var cityKey = _normalizeCityNameService.Normalize(photoUser.CityNomination.Value ?? string.Empty);
            var ratingKey = PhotoRedisKeys.RatingSortedSet(cityKey, season.Id);
            await _db.SortedSetRemoveAsync(ratingKey, photoUser.Id.ToString());
            await _db.KeyDeleteAsync(PhotoRedisKeys.PhotoHash(photoUser.Id));
            await _photoUserRepository.Delete(photoUser.Id);
        }
    }
}
