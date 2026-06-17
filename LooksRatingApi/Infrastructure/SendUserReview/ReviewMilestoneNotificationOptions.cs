namespace LooksRatingApi.Infrastructure.SendUserReview
{
    public sealed class ReviewMilestoneNotificationOptions
    {
        public bool Enabled { get; set; } = true;
        public int PendingBatchSize { get; set; } = 50;
    }
}
