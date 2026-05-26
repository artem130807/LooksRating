namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed class CreateReviewRequest
    {
        public long ReviewerTelegramId { get; set; }
        public Guid PhotoUserId { get; set; }
        public int Rating { get; set; }
    }
}
