namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IFeedCycleStore
    {
        Task<HashSet<Guid>> GetRatedProfileIdsAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default);

        Task<int> GetFeedRatingCounterAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default);

        Task EnsureCycleAnchorAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default);

        Task<DateTime> GetCycleAnchorAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default);

        Task ResetCycleAsync(
            Guid reviewerUserId,
            Guid seasonId,
            DateTime utcNow,
            CancellationToken cancellationToken = default);

        Task AddRatedProfileIdsAsync(
            Guid reviewerUserId,
            Guid seasonId,
            IReadOnlyCollection<Guid> profileIds,
            CancellationToken cancellationToken = default);

        Task MarkProfileAsServedAsync(
            Guid reviewerUserId,
            Guid seasonId,
            Guid profileId,
            CancellationToken cancellationToken = default);
    }
}
