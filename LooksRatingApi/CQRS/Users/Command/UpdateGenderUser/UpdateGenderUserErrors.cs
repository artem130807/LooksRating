namespace LooksRatingApi.CQRS.Users.Command.UpdateGenderUser
{
    public static class UpdateGenderUserErrors
    {
        public const string TelegramIdIsRequired = "TelegramIdIsRequired";
        public const string InvalidGender = "InvalidGender";
        public const string UserNotFound = "UserNotFound";
    }
}
