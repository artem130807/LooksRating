namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface IRatedProfileSparksRewardService
    {
        Task TryAwardForRatedProfileAsync(
            long ratedUserTelegramId,
            Guid ratedUserId,
            CancellationToken cancellationToken = default);
    }
}
