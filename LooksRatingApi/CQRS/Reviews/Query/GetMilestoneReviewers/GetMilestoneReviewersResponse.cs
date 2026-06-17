namespace LooksRatingApi.CQRS.Reviews.Query.GetMilestoneReviewers
{
    public sealed class GetMilestoneReviewersResponse
    {
        public Guid NotificationId { get; init; }
        public Guid PhotoProfileId { get; init; }
        public IReadOnlyList<MilestoneReviewerItem> Reviewers { get; init; } = Array.Empty<MilestoneReviewerItem>();
    }

    public sealed class MilestoneReviewerItem
    {
        public Guid ReviewerUserId { get; init; }
        public long ReviewerTelegramId { get; init; }
        public Guid? ReviewerPhotoProfileId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public int Rating { get; init; }
    }
}
