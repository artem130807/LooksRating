namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed class CreateReviewResult
    {
        public Guid ReviewId { get; init; }
        public Guid ReviewerUserId { get; init; }
        public Guid PhotoProfileId { get; init; }
        public int Rating { get; init; }
        public decimal UpdatedProfileRating { get; init; }
        public int UpdatedProfileRatingCount { get; init; }
    }
}
