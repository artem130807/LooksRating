namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public static class CreateReviewErrors
    {
        public const string ReviewerTelegramIdIsRequired = "ReviewerTelegramIdIsRequired";
        public const string PhotoProfileIdIsRequired = "PhotoProfileIdIsRequired";
        public const string InvalidRatingValue = "InvalidRatingValue";
        public const string ReviewerNotFound = "ReviewerNotFound";
        public const string PhotoProfileNotFound = "PhotoProfileNotFound";
        public const string SelfReviewIsNotAllowed = "SelfReviewIsNotAllowed";
        public const string ReviewAlreadyExists = "ReviewAlreadyExists";
        public const string InternalError = "InternalError";
    }
}
