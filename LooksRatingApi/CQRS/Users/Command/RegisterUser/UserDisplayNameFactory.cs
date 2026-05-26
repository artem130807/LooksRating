using CSharpFunctionalExtensions;
using LooksRatingApi.Services;

namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser
{
    internal static class UserDisplayNameFactory
    {
        public static Result<string?> Create(
            bool useTelegramUsernameAsDisplay,
            string? telegramUsername,
            string? customName)
        {
            if (useTelegramUsernameAsDisplay)
            {
                if (string.IsNullOrWhiteSpace(telegramUsername))
                {
                    return Result.Failure<string?>(RegisterUserErrors.TelegramUsernameRequiredForDisplay);
                }

                return Result.Success<string?>(null);
            }

            if (string.IsNullOrWhiteSpace(customName))
            {
                return Result.Failure<string?>(RegisterUserErrors.DisplayNameIsRequired);
            }

            var trimmed = customName.Trim();
            if (trimmed.Length is < 1 or > UserPublicDisplayName.MaxLength)
            {
                return Result.Failure<string?>(RegisterUserErrors.InvalidDisplayName);
            }

            return Result.Success<string?>(trimmed);
        }
    }
}
