namespace LooksRatingApi.Contracts
{
    public interface IVipExpirationReadService
    {
        Task<IReadOnlyDictionary<Guid, DateTime>> GetExpirationUtcByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default);
    }
}
