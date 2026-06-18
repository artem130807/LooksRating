using LooksRatingApi.Models;

namespace LooksRatingApi.Services
{
    public static class UserPublicDisplayName
    {
        public const string DefaultParticipant = "участник";
        public const int MaxLength = 32;

        public static string Resolve(User? user, string fallback = DefaultParticipant)
        {
            if (user is null)
            {
                return fallback;
            }

            if (!string.IsNullOrWhiteSpace(user.Name))
            {
                return user.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(user.TelegramUsername))
            {
                var username = user.TelegramUsername.Trim().TrimStart('@');
                return $"@{username}";
            }

            return fallback;
        }

        public static bool UsesTelegramUsernameAsDisplay(User? user) =>
            user is not null && string.IsNullOrWhiteSpace(user.Name);
    }
}
