using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public sealed class RecreateUserPhotoValidator : IRecreateUserPhotoValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly ISeasonRepository _seasonRepository;

        public RecreateUserPhotoValidator(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<string>> ValidateAsync(
            RecreateUserPhotoCommand command,
            CancellationToken cancellationToken)
        {
            if (command.Request.TelegramId <= 0)
            {
                return Result.Failure<string>(SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (string.IsNullOrWhiteSpace(command.Request.TelegramFileId))
            {
                return Result.Failure<string>(SetUserPhotoErrors.TelegramFileIdIsRequired);
            }

            if (command.Request.TelegramFileId.Trim().Length > 255)
            {
                return Result.Failure<string>(SetUserPhotoErrors.TelegramFileIdTooLong);
            }

            var user = await _userRepository.GetUserByTelegramId(command.Request.TelegramId);
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
                command.Request.TelegramId,
                season.Id,
                cancellationToken);
            if (existingPhoto is null)
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.PhotoNotFound);
            }

            return Result.Success(string.Empty);
        }
    }
}
