namespace LooksRatingApi.CQRS.Users.Command.UpdateUserAge
{
    public static class UpdateUserAgeErrors
    {
        public const string TelegramIdIsRequired = "TelegramIdIsRequired";
        public const string InvalidAge = "InvalidAge";
        public const string UserNotFound = "UserNotFound";
    }
}
