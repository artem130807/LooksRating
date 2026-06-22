namespace LooksRatingApi.CQRS.Seasons.Command.AckSeasonRolloverNotification
{
    public sealed class AckSeasonRolloverNotificationRequest
    {
        public string EventId { get; set; } = string.Empty;
        public List<long> RecipientTelegramIds { get; set; } = new();
    }
}
