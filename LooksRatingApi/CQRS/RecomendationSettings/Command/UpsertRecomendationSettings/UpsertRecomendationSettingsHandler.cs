using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.RecomendationSettings;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Models;
using MediatR;

namespace LooksRatingApi.CQRS.RecomendationSettings.Command.UpsertRecomendationSettings
{
    public sealed class UpsertRecomendationSettingsHandler
        : IRequestHandler<UpsertRecomendationSettingsCommand, Result<Unit>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRecomendationSettingsRepository _recomendationSettingsRepository;
        private readonly IUpsertRecomendationSettingsValidator _validator;

        public UpsertRecomendationSettingsHandler(
            IUserRepository userRepository,
            IRecomendationSettingsRepository recomendationSettingsRepository,
            IUpsertRecomendationSettingsValidator validator)
        {
            _userRepository = userRepository;
            _recomendationSettingsRepository = recomendationSettingsRepository;
            _validator = validator;
        }

        public async Task<Result<Unit>> Handle(
            UpsertRecomendationSettingsCommand command,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<Unit>(validationResult.Error);
            }

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null)
            {
                return Result.Failure<Unit>(RecomendationSettingsErrors.UserNotFound);
            }

            var normalizedCity = command.City.Trim().ToLowerInvariant();
            var cityResult = CityVo.Create(normalizedCity);
            if (cityResult.IsFailure)
            {
                return Result.Failure<Unit>(cityResult.Error);
            }

            var existing = user.RecomendationSettings
                ?? await _recomendationSettingsRepository.GetByUserIdAsync(user.Id, cancellationToken);

            if (existing is null)
            {
                var createResult = Models.RecomendationSettings.Create(
                    command.Age,
                    command.Gender,
                    cityResult.Value,
                    user.Id);
                if (createResult.IsFailure)
                {
                    return Result.Failure<Unit>(createResult.Error);
                }

                await _recomendationSettingsRepository.CreateAsync(createResult.Value, cancellationToken);
                return Result.Success(Unit.Value);
            }

            existing.UpdateAge(command.Age);
            existing.UpdateGender(command.Gender);
            existing.UpdateCity(cityResult.Value);
            await _recomendationSettingsRepository.UpdateAsync(existing, cancellationToken);

            return Result.Success(Unit.Value);
        }
    }
}
