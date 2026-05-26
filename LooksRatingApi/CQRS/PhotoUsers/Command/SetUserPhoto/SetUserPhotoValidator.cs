using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public sealed class SetUserPhotoValidator : ISetUserPhotoValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly ISeasonRepository _seasonRepository;

        public SetUserPhotoValidator(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<string>> ValidateAsync(SetUserPhotoCommand command, CancellationToken cancellationToken)
        {
            if (command.request.TelegramId <= 0)
            {
                return Result.Failure<string>(SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (string.IsNullOrWhiteSpace(command.request.TelegramFileId))
            {
                return Result.Failure<string>(SetUserPhotoErrors.TelegramFileIdIsRequired);
            }

            if (command.request.TelegramFileId.Trim().Length > 255)
            {
                return Result.Failure<string>(SetUserPhotoErrors.TelegramFileIdTooLong);
            }

            var user = await _userRepository.GetUserByTelegramId(command.request.TelegramId);
            if (user is null)
            {
                return Result.Failure<string>(SetUserPhotoErrors.UserNotFound);
            }

            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return Result.Failure<string>(SetUserPhotoErrors.CurrentSeasonNotFound);
            }

            var existingPhoto = await _photoUserRepository.GetByTelegramIdAndSeasonIdAsync(
                command.request.TelegramId,
                season.Id,
                cancellationToken);
            if (existingPhoto is not null)
            {
                return Result.Failure<string>(SetUserPhotoErrors.PhotoAlreadyExists);
            }

            return Result.Success(string.Empty);
        }
    }
}
