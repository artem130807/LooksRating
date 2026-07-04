namespace LooksRatingApi.Contracts.ReviewContracts
{
    public interface IReviewBackgroundService
    {
        Task ProcessOutboxAsync(Guid outboxId, CancellationToken cancellationToken);
        Task EnqueuePendingOutboxAsync(CancellationToken cancellationToken);
    }
}