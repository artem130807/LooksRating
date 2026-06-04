namespace LooksRatingApi.Contracts
{
    public interface IVipTopRewardOrchestrator
    {
        Task<IReadOnlyList<VipTopProfileCandidate>> ProcessAndGetProfilesAsync(
            CancellationToken cancellationToken = default);
    }
}
