namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public static class SetUserPhotoErrors
    {
        public const string TelegramIdIsRequired = "TelegramIdIsRequired";
        public const string TelegramFileIdIsRequired = "TelegramFileIdIsRequired";
        public const string TelegramFileIdTooLong = "TelegramFileIdTooLong";
        public const string UserNotFound = "UserNotFound";
        public const string CurrentSeasonNotFound = "CurrentSeasonNotFound";
        public const string PhotoAlreadyExists = "PhotoAlreadyExists";
        public const string UserProfileIncomplete = "UserProfileIncomplete";
        public const string InvalidNominationCity = "InvalidNominationCity";
        public const string InvalidNominationAge = "InvalidNominationAge";
        public const string InvalidNominationGender = "InvalidNominationGender";
        public const string VipPhotoLimitExceeded = "VipPhotoLimitExceeded";
        public const string PhotoUploadInProgress = "PhotoUploadInProgress";
    }
}
