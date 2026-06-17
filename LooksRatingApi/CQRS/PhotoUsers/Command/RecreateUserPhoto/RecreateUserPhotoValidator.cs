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
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;

        public RecreateUserPhotoValidator(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
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

            var profile = await _photoProfileRepository.GetByUserAndSeasonAsync(
                user.Id,
                season.Id,
                cancellationToken);
            if (profile is null || profile.Photos.Count == 0)
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.PhotoNotFound);
            }

            if (command.Request.TargetPhotoId.HasValue
                && profile.Photos.All(x => x.Id != command.Request.TargetPhotoId.Value))
            {
                return Result.Failure<string>(RecreateUserPhotoErrors.TargetPhotoNotFound);
            }

            return Result.Success(string.Empty);
        }
    }
}
