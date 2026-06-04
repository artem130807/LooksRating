namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public static class RecreateUserPhotoErrors
    {
        public const string PhotoNotFound = "PhotoNotFound";
        public const string TargetPhotoNotFound = "TargetPhotoNotFound";
        public const string TooManyPhotosForNonVip = "TooManyPhotosForNonVip";
        public const string TooManyPhotosForVip = "TooManyPhotosForVip";
        public const string PhotoIdsRequired = "PhotoIdsRequired";
    }
}
