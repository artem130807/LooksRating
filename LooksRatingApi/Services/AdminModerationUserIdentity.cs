using LooksRatingApi.Models;

namespace LooksRatingApi.Services
{
    /// <summary>
    /// Идентификатор пользователя только для внутренней панели модерации.
    /// Не использовать в публичных API.
    /// </summary>
    public static class AdminModerationUserIdentity
    {
        public static string FormatLabel(User? user, string fallback = UserPublicDisplayName.DefaultParticipant)
        {
            if (user is null)
            {
                return fallback;
            }

            if (!string.IsNullOrWhiteSpace(user.TelegramUsername))
            {
                var username = user.TelegramUsername.Trim().TrimStart('@');
                return $"@{username}";
            }

            if (!string.IsNullOrWhiteSpace(user.Name))
            {
                return $"{user.Name.Trim()} · ID {user.TelegramId}";
            }

            if (user.TelegramId != 0)
            {
                return $"ID {user.TelegramId}";
            }

            return fallback;
        }
    }
}
