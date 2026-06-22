namespace LooksRatingApi.Infrastructure.SeasonNotifications
{
    public sealed class SeasonRolloverNotificationOptions
    {
        public bool Enabled { get; set; } = true;
        public int PendingBatchSize { get; set; } = 50;
        public int EnqueueBatchSize { get; set; } = 500;
        public int TtlDays { get; set; } = 45;
    }
}
