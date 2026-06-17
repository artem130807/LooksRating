using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.RecomendationSettings;
using LooksRatingApi.Domain.Vo;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserAge
{
    public sealed class UpdateUserAgeCommandHandler : IRequestHandler<UpdateUserAgeCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRecomendationSettingsRepository _recomendationSettingsRepository;
        private readonly IUpdateUserAgeValidator _validator;

        public UpdateUserAgeCommandHandler(
            IUserRepository userRepository,
            IRecomendationSettingsRepository recomendationSettingsRepository,
            IUpdateUserAgeValidator validator)
        {
            _userRepository = userRepository;
            _recomendationSettingsRepository = recomendationSettingsRepository;
            _validator = validator;
        }

        public async Task<Result<string>> Handle(UpdateUserAgeCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<string>(validationResult.Error);
            }

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null)
            {
                return Result.Failure<string>(UpdateUserAgeErrors.UserNotFound);
            }

            var settings = user.RecomendationSettings
                ?? await _recomendationSettingsRepository.GetByUserIdAsync(user.Id, cancellationToken);
            if (settings is null)
            {
                return Result.Failure<string>(RecomendationSettingsErrors.SettingsNotFound);
            }

            settings.UpdateAge(command.Age);
            await _recomendationSettingsRepository.UpdateAsync(settings, cancellationToken);

            return Result.Success("Успешно");
        }
    }
}
