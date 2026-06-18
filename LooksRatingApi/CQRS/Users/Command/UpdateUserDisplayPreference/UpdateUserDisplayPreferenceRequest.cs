namespace LooksRatingApi.CQRS.Users.Command.UpdateUserDisplayPreference
{
    public sealed class UpdateUserDisplayPreferenceRequest
    {
        public long TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public bool UseTelegramUsernameAsDisplay { get; set; }
        public string? CustomName { get; set; }
    }
}
