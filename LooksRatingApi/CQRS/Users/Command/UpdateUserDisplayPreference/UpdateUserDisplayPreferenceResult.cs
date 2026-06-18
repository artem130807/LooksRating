namespace LooksRatingApi.CQRS.Users.Command.UpdateUserDisplayPreference
{
    public sealed class UpdateUserDisplayPreferenceResult
    {
        public string DisplayName { get; init; } = string.Empty;
        public bool UsesTelegramUsernameAsDisplay { get; init; }
    }
}
