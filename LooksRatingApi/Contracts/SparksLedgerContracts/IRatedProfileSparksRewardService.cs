namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface IRatedProfileSparksRewardService
    {
        Task<bool> TryAwardForRatedProfileAsync(
            long ratedUserTelegramId,
            Guid ratedUserId,
            CancellationToken cancellationToken = default);
    }
}
