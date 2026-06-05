namespace LooksRatingApi.Infrastructure.DistributedLock
{
    public interface IRedisDistributedLock
    {
        Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken = default);

        Task<IRedisDistributedLockHandle?> TryAcquireAsync(
            string key,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);
    }

    public interface IRedisDistributedLockHandle : IAsyncDisposable
    {
        string Key { get; }
    }
}
