using LooksRatingApi.Services.SeasonLifecycle;

namespace LooksRatingApi.Contracts.SeasonLifecycle
{
    public interface ISeasonRolloverNotificationStore
    {
        Task<int> TryEnqueueBatchAsync(
            SeasonRolloverEnqueueRequest request,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SeasonRolloverPendingBatch>> GetPendingBatchesAsync(
            int limit,
            CancellationToken cancellationToken = default);

        Task AckDeliveredAsync(
            string eventId,
            IReadOnlyList<long> recipientTelegramIds,
            CancellationToken cancellationToken = default);
    }
}
