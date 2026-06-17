namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface IReviewSparksRewardService
    {
        Task TryAwardForReviewAsync(
            long reviewerTelegramId,
            Guid reviewerUserId,
            CancellationToken cancellationToken = default);
    }
}
