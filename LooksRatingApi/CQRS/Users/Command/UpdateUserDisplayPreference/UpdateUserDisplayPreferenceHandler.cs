using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.Users.Command.RegisterUser;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserDisplayPreference
{
    public sealed class UpdateUserDisplayPreferenceHandler
        : IRequestHandler<UpdateUserDisplayPreferenceCommand, Result<UpdateUserDisplayPreferenceResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IPhotoRatingCacheService _photoRatingCacheService;

        public UpdateUserDisplayPreferenceHandler(
            IUserRepository userRepository,
            ISeasonRepository seasonRepository,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoRatingCacheService photoRatingCacheService)
        {
            _userRepository = userRepository;
            _seasonRepository = seasonRepository;
            _photoProfileRepository = photoProfileRepository;
            _photoRatingCacheService = photoRatingCacheService;
        }

        public async Task<Result<UpdateUserDisplayPreferenceResult>> Handle(
            UpdateUserDisplayPreferenceCommand command,
            CancellationToken cancellationToken)
        {
            if (command.TelegramId <= 0)
            {
                return Result.Failure<UpdateUserDisplayPreferenceResult>(
                    UpdateUserDisplayPreferenceErrors.TelegramIdIsRequired);
            }

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null)
            {
                return Result.Failure<UpdateUserDisplayPreferenceResult>(
                    UpdateUserDisplayPreferenceErrors.UserNotFound);
            }

            var telegramUsername = ResolveTelegramUsername(command.TelegramUsername, user.TelegramUsername);
            var displayNameResult = UserDisplayNameFactory.Create(
                command.UseTelegramUsernameAsDisplay,
                telegramUsername,
                command.CustomName);
            if (displayNameResult.IsFailure)
            {
                return Result.Failure<UpdateUserDisplayPreferenceResult>(displayNameResult.Error);
            }

            user.TelegramUsername = telegramUsername;
            user.Name = displayNameResult.Value;
            await _userRepository.Update(user);

            await SyncCurrentSeasonProfileDisplayNamesAsync(
                command.TelegramId,
                user,
                cancellationToken);

            return Result.Success(new UpdateUserDisplayPreferenceResult
            {
                DisplayName = UserPublicDisplayName.Resolve(user),
                UsesTelegramUsernameAsDisplay = UserPublicDisplayName.UsesTelegramUsernameAsDisplay(user),
            });
        }

        private async Task SyncCurrentSeasonProfileDisplayNamesAsync(
            long telegramId,
            User user,
            CancellationToken cancellationToken)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return;
            }

            var profiles = await _photoProfileRepository.GetByTelegramAndSeasonListAsync(
                telegramId,
                season.Id,
                cancellationToken);
            if (profiles.Count == 0)
            {
                return;
            }

            var displayName = UserPublicDisplayName.Resolve(user);
            foreach (var profile in profiles)
            {
                await _photoRatingCacheService.SyncProfileDisplayNameAsync(
                    profile.Id,
                    displayName,
                    cancellationToken);
            }
        }

        private static string? ResolveTelegramUsername(string? requestedUsername, string? storedUsername)
        {
            if (!string.IsNullOrWhiteSpace(requestedUsername))
            {
                return requestedUsername.Trim().TrimStart('@');
            }

            return string.IsNullOrWhiteSpace(storedUsername)
                ? null
                : storedUsername.Trim().TrimStart('@');
        }
    }
}
