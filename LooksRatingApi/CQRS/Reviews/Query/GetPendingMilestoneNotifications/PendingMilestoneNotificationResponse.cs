namespace LooksRatingApi.CQRS.Reviews.Query.GetPendingMilestoneNotifications
{
    public sealed class PendingMilestoneNotificationResponse
    {
        public Guid Id { get; init; }
        public Guid PhotoProfileId { get; init; }
        public long OwnerTelegramId { get; init; }
        public int CycleNumber { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
