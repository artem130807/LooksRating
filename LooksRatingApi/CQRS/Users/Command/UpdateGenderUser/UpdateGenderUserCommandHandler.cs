using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.RecomendationSettings;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateGenderUser
{
    public sealed class UpdateGenderUserCommandHandler
        : IRequestHandler<UpdateGenderUserCommandCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRecomendationSettingsRepository _recomendationSettingsRepository;
        private readonly IUpdateGenderUserValidator _validator;

        public UpdateGenderUserCommandHandler(
            IUserRepository userRepository,
            IRecomendationSettingsRepository recomendationSettingsRepository,
            IUpdateGenderUserValidator validator)
        {
            _userRepository = userRepository;
            _recomendationSettingsRepository = recomendationSettingsRepository;
            _validator = validator;
        }

        public async Task<Result<string>> Handle(
            UpdateGenderUserCommandCommand command,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<string>(validationResult.Error);
            }

            var user = await _userRepository.GetUserByTelegramId(command.telegramId);
            if (user is null)
            {
                return Result.Failure<string>(UpdateGenderUserErrors.UserNotFound);
            }

            var settings = user.RecomendationSettings
                ?? await _recomendationSettingsRepository.GetByUserIdAsync(user.Id, cancellationToken);
            if (settings is null)
            {
                return Result.Failure<string>(RecomendationSettingsErrors.SettingsNotFound);
            }

            settings.UpdateGender(command.gender);
            await _recomendationSettingsRepository.UpdateAsync(settings, cancellationToken);

            return Result.Success("Успешно");
        }
    }
}
