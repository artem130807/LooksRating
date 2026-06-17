namespace LooksRatingApi.Contracts
{
    public interface IGetTopVipService
    {
        Task<IReadOnlyList<VipTopProfileCandidate>> GetCandidates(CancellationToken cancellationToken = default);
    }
}
