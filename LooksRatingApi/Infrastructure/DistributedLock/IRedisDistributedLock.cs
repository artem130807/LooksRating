namespace LooksRatingApi.Infrastructure.DistributedLock
{
    public interface IRedisDistributedLock
    {
        Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken = default);

        Task<IRedisDistributedLockHandle?> TryAcquireAsync(
            string key,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);

        Task<bool> ReleaseAsync(
            string key,
            string lockToken,
            CancellationToken cancellationToken = default);

        Task<bool> RenewAsync(
            string key,
            string lockToken,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);
    }

    public interface IRedisDistributedLockHandle : IAsyncDisposable
    {
        string Key { get; }

        string Token { get; }

        Task<bool> RenewAsync(TimeSpan ttl, CancellationToken cancellationToken = default);
    }
}
