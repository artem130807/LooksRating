using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    internal static class PhotoNominationResolver
    {
        public static async Task<Result<(int Age, GenderEnum Gender, CityVo City)>> ResolveAsync(
            User user,
            PhotoNominationRequest nomination,
            ICityService cityService,
            INormalizeCityNameService normalizeCityNameService)
        {
            if (nomination.Age is null || !TopService.IsValidNominationAge(nomination.Age.Value))
            {
                return Result.Failure<(int, GenderEnum, CityVo)>(SetUserPhotoErrors.InvalidNominationAge);
            }

            if (nomination.Gender is null
                || !Enum.IsDefined(typeof(GenderEnum), nomination.Gender.Value)
                || nomination.Gender.Value == GenderEnum.Unknown)
            {
                return Result.Failure<(int, GenderEnum, CityVo)>(SetUserPhotoErrors.InvalidNominationGender);
            }

            if (string.IsNullOrWhiteSpace(nomination.City))
            {
                return Result.Failure<(int, GenderEnum, CityVo)>(SetUserPhotoErrors.InvalidNominationCity);
            }

            var normalizedCity = nomination.City.Trim().ToLowerInvariant();
            if (!cityService.IsCityValid(normalizedCity))
            {
                return Result.Failure<(int, GenderEnum, CityVo)>(SetUserPhotoErrors.InvalidNominationCity);
            }

            var cityResult = CityVo.Create(normalizedCity);
            if (cityResult.IsFailure)
            {
                return Result.Failure<(int, GenderEnum, CityVo)>(cityResult.Error);
            }

            return Result.Success((nomination.Age.Value, nomination.Gender.Value, cityResult.Value));
        }
    }
}
