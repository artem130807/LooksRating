namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed class CreateReviewResult
    {
        public Guid ReviewId { get; init; }
        public Guid ReviewerUserId { get; init; }
        public Guid PhotoUserId { get; init; }
        public int Rating { get; init; }
        public decimal UpdatedPhotoRating { get; init; }
        public int UpdatedPhotoRatingCount { get; init; }
    }
}
