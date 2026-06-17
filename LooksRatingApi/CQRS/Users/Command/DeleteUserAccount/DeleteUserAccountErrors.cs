namespace LooksRatingApi.CQRS.Users.Command.DeleteUserAccount
{
    public static class DeleteUserAccountErrors
    {
        public const string TelegramIdIsRequired = "TelegramIdIsRequired";
        public const string UserNotFound = "UserNotFound";
    }
}
