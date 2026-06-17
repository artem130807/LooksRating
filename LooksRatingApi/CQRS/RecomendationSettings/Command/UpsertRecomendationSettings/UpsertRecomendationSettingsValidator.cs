using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.RecomendationSettings;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.RecomendationSettings.Command.UpsertRecomendationSettings
{
    public interface IUpsertRecomendationSettingsValidator
    {
        Task<Result<string>> ValidateAsync(
            UpsertRecomendationSettingsCommand command,
            CancellationToken cancellationToken);
    }

    public sealed class UpsertRecomendationSettingsValidator : IUpsertRecomendationSettingsValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly ICityService _cityService;

        public UpsertRecomendationSettingsValidator(
            IUserRepository userRepository,
            ICityService cityService)
        {
            _userRepository = userRepository;
            _cityService = cityService;
        }

        public async Task<Result<string>> ValidateAsync(
            UpsertRecomendationSettingsCommand command,
            CancellationToken cancellationToken)
        {
            if (command.TelegramId <= 0)
            {
                return Result.Failure<string>(RecomendationSettingsErrors.TelegramIdIsRequired);
            }

            if (command.Age is < TopService.AllAges or > 100)
            {
                return Result.Failure<string>(RecomendationSettingsErrors.InvalidAge);
            }

            if (!Enum.IsDefined(typeof(GenderEnum), command.Gender) || command.Gender == GenderEnum.Unknown)
            {
                return Result.Failure<string>(RecomendationSettingsErrors.InvalidGender);
            }

            if (string.IsNullOrWhiteSpace(command.City))
            {
                return Result.Failure<string>(RecomendationSettingsErrors.InvalidCity);
            }

            var normalizedCity = command.City.Trim().ToLowerInvariant();
            if (!_cityService.IsCityValid(normalizedCity))
            {
                return Result.Failure<string>(RecomendationSettingsErrors.InvalidCity);
            }

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null)
            {
                return Result.Failure<string>(RecomendationSettingsErrors.UserNotFound);
            }

            return Result.Success(string.Empty);
        }
    }
}
