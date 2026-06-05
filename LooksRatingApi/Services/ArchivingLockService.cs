using LooksRatingApi.Infrastructure.DistributedLock;

namespace LooksRatingApi.Services
{
    public sealed class ArchivingLockService
    {
        private readonly IRedisDistributedLock _distributedLock;

        public ArchivingLockService(IRedisDistributedLock distributedLock)
        {
            _distributedLock = distributedLock;
        }

        public Task<bool> IsArchivingInProgressAsync(CancellationToken cancellationToken = default) =>
            _distributedLock.IsLockedAsync(DistributedLockKeys.Archive, cancellationToken);

        public Task<IRedisDistributedLockHandle?> TryAcquireAsync(
            TimeSpan ttl,
            CancellationToken cancellationToken = default) =>
            _distributedLock.TryAcquireAsync(DistributedLockKeys.Archive, ttl, cancellationToken);
    }
}
