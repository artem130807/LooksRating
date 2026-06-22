namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class SeasonRolloverEnqueueRequest
    {
        public Guid ClosedSeasonId { get; init; }
        public string ClosedSeasonName { get; init; } = string.Empty;
        public int ClosedSeasonNumber { get; init; }
        public Guid NewSeasonId { get; init; }
        public string NewSeasonName { get; init; } = string.Empty;
        public int NewSeasonNumber { get; init; }
        public IReadOnlyList<long> RecipientTelegramIds { get; init; } = Array.Empty<long>();
    }
}
