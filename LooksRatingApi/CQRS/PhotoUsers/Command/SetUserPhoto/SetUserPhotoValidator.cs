using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;

using LooksRatingApi.Services.PhotoProfiles;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public sealed class SetUserPhotoValidator : ISetUserPhotoValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;

        public SetUserPhotoValidator(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
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

            var profile = await _photoProfileRepository.GetByUserAndSeasonAsync(
                user.Id,
                season.Id,
                cancellationToken);
            if (profile is not null && user.Status == VipStatus.Unavaillable)
            {
                return Result.Failure<string>(SetUserPhotoErrors.PhotoAlreadyExists);
            }

            if (profile is not null
                && !PhotoProfileLimits.CanAddPhoto(profile.Photos.Count, user.Status))
            {
                return Result.Failure<string>(SetUserPhotoErrors.VipPhotoLimitExceeded);
            }

            return Result.Success(string.Empty);
        }
    }
}
