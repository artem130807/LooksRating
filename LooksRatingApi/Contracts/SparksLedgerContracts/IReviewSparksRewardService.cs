namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface IReviewSparksRewardService
    {
        Task<bool> TryAwardForReviewAsync(
            long reviewerTelegramId,
            Guid reviewerUserId,
            CancellationToken cancellationToken = default);
    }
}
