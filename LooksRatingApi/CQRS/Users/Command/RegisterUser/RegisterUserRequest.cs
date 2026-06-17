using LooksRatingApi.Enums;

namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser
{
    public sealed class RegisterUserRequest
    {
        public long TelegramId { get; set; }

        public string? TelegramUsername { get; set; }

        public bool UseTelegramUsernameAsDisplay { get; set; }

        public string? Name { get; set; }

        public string? Link { get; set; }
    }
}
