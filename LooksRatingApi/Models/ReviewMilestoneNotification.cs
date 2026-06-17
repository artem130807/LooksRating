using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public sealed class ReviewMilestoneNotification
    {
        public Guid Id { get; private set; }
        public Guid PhotoProfileId { get; private set; }
        public long OwnerTelegramId { get; private set; }
        public int CycleNumber { get; private set; }
        public ReviewMilestoneNotificationStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? SentAt { get; private set; }

        private ReviewMilestoneNotification()
        {
        }

        public static ReviewMilestoneNotification CreatePending(
            Guid photoProfileId,
            long ownerTelegramId,
            int cycleNumber)
        {
            return new ReviewMilestoneNotification
            {
                Id = Guid.NewGuid(),
                PhotoProfileId = photoProfileId,
                OwnerTelegramId = ownerTelegramId,
                CycleNumber = cycleNumber,
                Status = ReviewMilestoneNotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void MarkSent()
        {
            Status = ReviewMilestoneNotificationStatus.Sent;
            SentAt = DateTime.UtcNow;
        }
    }
}
