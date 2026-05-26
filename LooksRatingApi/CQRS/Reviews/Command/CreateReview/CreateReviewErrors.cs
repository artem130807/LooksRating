namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public static class CreateReviewErrors
    {
        public const string ReviewerTelegramIdIsRequired = "ReviewerTelegramIdIsRequired";
        public const string PhotoUserIdIsRequired = "PhotoUserIdIsRequired";
        public const string InvalidRatingValue = "InvalidRatingValue";
        public const string ReviewerNotFound = "ReviewerNotFound";
        public const string PhotoUserNotFound = "PhotoUserNotFound";
        public const string SelfReviewIsNotAllowed = "SelfReviewIsNotAllowed";
        public const string ReviewAlreadyExists = "ReviewAlreadyExists";
    }
}
