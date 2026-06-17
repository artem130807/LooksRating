using LooksRatingApi.Infrastructure.DistributedLock;

namespace LooksRatingApi.Services
{
    public sealed class ArchivingLockService
    {
        public static readonly TimeSpan LockSlice = TimeSpan.FromMinutes(5);

        private readonly IRedisDistributedLock _distributedLock;

        public ArchivingLockService(IRedisDistributedLock distributedLock)
        {
            _distributedLock = distributedLock;
        }

        public Task<bool> IsArchivingInProgressAsync(CancellationToken cancellationToken = default) =>
            _distributedLock.IsLockedAsync(DistributedLockKeys.Archive, cancellationToken);

        public Task<IRedisDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken = default) =>
            _distributedLock.TryAcquireAsync(DistributedLockKeys.Archive, LockSlice, cancellationToken);

        public Task<bool> RenewAsync(
            IRedisDistributedLockHandle handle,
            CancellationToken cancellationToken = default) =>
            handle.RenewAsync(LockSlice, cancellationToken);
    }
}
