using LooksRatingApi.Infrastructure.DistributedLock;

namespace LooksRatingApi.Services
{
    public sealed class TheBestWeekLockService
    {
        private readonly IRedisDistributedLock _distributedLock;

        public TheBestWeekLockService(IRedisDistributedLock distributedLock)
        {
            _distributedLock = distributedLock;
        }

        public Task<bool> IsRefreshInProgressAsync(CancellationToken cancellationToken = default) =>
            _distributedLock.IsLockedAsync(DistributedLockKeys.TheBestWeekRefresh, cancellationToken);

        public Task<IRedisDistributedLockHandle?> TryAcquireAsync(
            TimeSpan ttl,
            CancellationToken cancellationToken = default) =>
            _distributedLock.TryAcquireAsync(DistributedLockKeys.TheBestWeekRefresh, ttl, cancellationToken);
    }
}
