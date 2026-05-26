namespace LooksRatingApi.CQRS.Users.Command.UpdateUserCity
{
    public static class UpdateUserCityErrors
    {
        public const string TelegramIdIsRequired = "TelegramIdIsRequired";
        public const string InvalidCity = "InvalidCity";
        public const string UserNotFound = "UserNotFound";
    }
}
