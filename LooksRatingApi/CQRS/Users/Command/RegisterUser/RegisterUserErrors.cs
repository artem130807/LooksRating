namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser
{
    public static class RegisterUserErrors
    {
        public const string TelegramIdIsRequired = "TelegramIdIsRequired";
        public const string InvalidAge = "InvalidAge";
        public const string InvalidGender = "InvalidGender";
        public const string InvalidCity = "InvalidCity";
        public const string UserAlreadyExists = "UserAlreadyExists";
        public const string InvalidTelegramUsername = "InvalidTelegramUsername";
        public const string DisplayNameIsRequired = "DisplayNameIsRequired";
        public const string InvalidDisplayName = "InvalidDisplayName";
        public const string TelegramUsernameRequiredForDisplay = "TelegramUsernameRequiredForDisplay";
        public const string RegistrationFailed = "RegistrationFailed";
    }
}
