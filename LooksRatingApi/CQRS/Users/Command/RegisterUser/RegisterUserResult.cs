namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser

{

    public sealed class RegisterUserResult

    {

        public Guid UserId { get; init; }

        public long TelegramId { get; init; }

        public string? TelegramUsername { get; init; }

        public string DisplayName { get; init; } = string.Empty;

    }

}

