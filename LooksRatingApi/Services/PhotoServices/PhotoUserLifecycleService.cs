using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
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
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly IDatabase _db;

        public PhotoUserLifecycleService(
            IPhotoUserRepository photoUserRepository,
            IPhotoProfileRepository photoProfileRepository,
            INormalizeCityNameService normalizeCityNameService,
            IConnectionMultiplexer redis)
        {
            _photoUserRepository = photoUserRepository;
            _photoProfileRepository = photoProfileRepository;
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
            var lockKey = $"photo-profile:create:{season.Id:N}:{user.Id:N}";
            var lockToken = Guid.NewGuid().ToString("N");
            var lockAcquired = await _db.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(10));
            if (!lockAcquired)
            {
                throw new InvalidOperationException(SetUserPhotoErrors.PhotoUploadInProgress);
            }

            try
            {
            PhotoProfile? profile = null;
            var attempts = 0;

            while (attempts < 3)
            {
                attempts++;
                profile = await _photoProfileRepository.GetByUserAndSeasonAsync(user.Id, season.Id, cancellationToken);
                if (profile is null)
                {
                    profile = new PhotoProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        SeasonId = season.Id,
                        Rank = RankEnum.Terrible,
                        AgeNomination = ageNomination,
                        GenderNomination = genderNomination,
                        CityNomination = cityNomination,
                        Status = StatusEnum.Active,
                        Rating = 0m,
                        RatingCount = 0,
                        CreatedAt = DateTime.UtcNow,
                    };
                    profile.Photos.Add(new PhotoProfilePhoto
                    {
                        Id = Guid.NewGuid(),
                        PhotoProfileId = profile.Id,
                        TelegramFileId = telegramFileId,
                        SortOrder = 0,
                        CreatedAt = DateTime.UtcNow,
                    });
                    try
                    {
                        await _photoProfileRepository.CreateAsync(profile, cancellationToken);
                        break;
                    }
                    catch (DbUpdateException) when (attempts < 3)
                    {
                        continue;
                    }
                }
                else
                {
                    try
                    {
                        await _photoProfileRepository.AddPhotoAsync(profile.Id, telegramFileId, cancellationToken);
                        break;
                    }
                    catch (InvalidOperationException ex) when (ex.Message == SetUserPhotoErrors.VipPhotoLimitExceeded)
                    {
                        throw;
                    }
                    catch (DbUpdateConcurrencyException) when (attempts < 3)
                    {
                        continue;
                    }
                    catch (DbUpdateException) when (attempts < 3)
                    {
                        continue;
                    }
                }
            }

            if (profile is null)
            {
                throw new InvalidOperationException("PhotoProfile could not be created or loaded.");
            }

            var photoUser = new PhotoUser
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PhotoProfileId = profile.Id,
                TelegramFileId = telegramFileId,
                Rank = profile.Rank,
                AgeNomination = ageNomination,
                GenderNomination = genderNomination,
                CityNomination = cityNomination,
                SeasonId = season.Id,
                Status = StatusEnum.Active,
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
            };
            var legacyAttempts = 0;
            while (legacyAttempts < 3)
            {
                legacyAttempts++;
                try
                {
                    await _photoUserRepository.Create(photoUser);
                    break;
                }
                catch (DbUpdateException) when (legacyAttempts < 3)
                {
                    continue;
                }
            }
            if (legacyAttempts >= 3)
            {
                throw new InvalidOperationException(SetUserPhotoErrors.PhotoUploadInProgress);
            }

            var photoKey = PhotoRedisKeys.ProfileHash(profile.Id);
            await _db.HashSetAsync(photoKey, new HashEntry[]
            {
                new("name", UserPublicDisplayName.Resolve(user)),
                new("rating", profile.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("rating_count", profile.RatingCount),
                new("gender_photo", profile.GenderNomination.ToString()),
                new("age_photo", profile.AgeNomination),
                new("user_id", profile.UserId.ToString())
            });

            var cityKey = _normalizeCityNameService.Normalize(profile.CityNomination.Value ?? string.Empty);
            var sortedSetKey = PhotoRedisKeys.RatingSortedSet(cityKey, season.Id);
            await _db.SortedSetAddAsync(
                sortedSetKey,
                profile.Id.ToString(),
                PhotoRankingScore.ToSortScore(profile.Rating, profile.RatingCount));
            return photoUser;
            }
            finally
            {
                await _db.LockReleaseAsync(lockKey, lockToken);
            }
        }

        public async Task RemoveAsync(PhotoUser photoUser, Season season, CancellationToken cancellationToken)
        {
            if (photoUser.PhotoProfileId.HasValue)
            {
                var profile = await _photoProfileRepository.GetByIdAsync(photoUser.PhotoProfileId.Value, cancellationToken);
                if (profile is not null)
                {
                    var cityKey = _normalizeCityNameService.Normalize(profile.CityNomination.Value ?? string.Empty);
                    var ratingKey = PhotoRedisKeys.RatingSortedSet(cityKey, season.Id);
                    await _db.SortedSetRemoveAsync(ratingKey, profile.Id.ToString());
                    await _db.KeyDeleteAsync(PhotoRedisKeys.ProfileHash(profile.Id));
                    await _photoProfileRepository.DeleteAsync(profile.Id, cancellationToken);
                }
            }
            await _photoUserRepository.Delete(photoUser.Id);
        }
    }
}
