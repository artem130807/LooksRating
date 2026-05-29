using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.RecomendationSettings;
using LooksRatingApi.Domain.Vo;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserCity
{
    public sealed class UpdateUserCityCommandHandler : IRequestHandler<UpdateUserCityCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRecomendationSettingsRepository _recomendationSettingsRepository;
        private readonly IUpdateUserCityValidator _validator;
        private readonly ICityService _cityService;

        public UpdateUserCityCommandHandler(
            IUserRepository userRepository,
            IRecomendationSettingsRepository recomendationSettingsRepository,
            IUpdateUserCityValidator validator,
            ICityService cityService)
        {
            _userRepository = userRepository;
            _recomendationSettingsRepository = recomendationSettingsRepository;
            _validator = validator;
            _cityService = cityService;
        }

        public async Task<Result<string>> Handle(UpdateUserCityCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<string>(validationResult.Error);
            }

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null)
            {
                return Result.Failure<string>(UpdateUserCityErrors.UserNotFound);
            }

            var settings = user.RecomendationSettings
                ?? await _recomendationSettingsRepository.GetByUserIdAsync(user.Id, cancellationToken);
            if (settings is null)
            {
                return Result.Failure<string>(RecomendationSettingsErrors.SettingsNotFound);
            }

            if (!_cityService.TryResolveCanonicalCity(command.City, out var canonicalCity))
            {
                return Result.Failure<string>(UpdateUserCityErrors.InvalidCity);
            }

            var cityResult = CityVo.Create(canonicalCity);
            if (cityResult.IsFailure)
            {
                return Result.Failure<string>(cityResult.Error);
            }

            settings.UpdateCity(cityResult.Value);
            await _recomendationSettingsRepository.UpdateAsync(settings, cancellationToken);

            return Result.Success("Успешно");
        }
    }
}
