using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Services.PhotoProfiles;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public sealed class RecreateAllUserPhotosValidator : IRecreateAllUserPhotosValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;

        public RecreateAllUserPhotosValidator(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<string>> ValidateAsync(
            RecreateAllUserPhotosCommand command,
            CancellationToken cancellationToken)
        {
            if (command.Request.TelegramId <= 0)
            {
                return Result.Failure<string>(SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (command.Request.TelegramFileIds is null || command.Request.TelegramFileIds.Count == 0)
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.PhotoIdsRequired);
            }

            var trimmed = command.Request.TelegramFileIds
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            if (trimmed.Count == 0)
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.PhotoIdsRequired);
            }

            if (trimmed.Any(x => x.Length > 255))
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

            var profile = await _photoProfileRepository.GetByUserAndSeasonAsync(
                user.Id,
                season.Id,
                cancellationToken);
            if (profile is null || profile.Photos.Count == 0)
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.PhotoNotFound);
            }

            var isVip = user.Status == Enums.VipStatus.Availlable;
            if (!isVip && trimmed.Count > 1)
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.TooManyPhotosForNonVip);
            }

            if (isVip && trimmed.Count > PhotoProfileLimits.VipMaxPhotos)
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.TooManyPhotosForVip);
            }

            return Result.Success(string.Empty);
        }
    }
}
