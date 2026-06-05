using LooksRatingApi.Contracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.DistributedLock;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services
{
    public sealed class VipStatusExpiryProcessor : IVipStatusExpiryProcessor
    {
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(50);

        private readonly LooksRatingDbContext _context;
        private readonly IVipExpirationReadService _vipExpirationReadService;
        private readonly IRedisDistributedLock _distributedLock;
        private readonly ILogger<VipStatusExpiryProcessor> _logger;

        public VipStatusExpiryProcessor(
            LooksRatingDbContext context,
            IVipExpirationReadService vipExpirationReadService,
            IRedisDistributedLock distributedLock,
            ILogger<VipStatusExpiryProcessor> logger)
        {
            _context = context;
            _vipExpirationReadService = vipExpirationReadService;
            _distributedLock = distributedLock;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            await using var lockHandle = await _distributedLock.TryAcquireAsync(
                DistributedLockKeys.VipStatusExpiry,
                LockTtl,
                cancellationToken);

            if (lockHandle is null)
            {
                _logger.LogDebug("VIP expiry пропущен: выполняется на другом инстансе");
                return;
            }

            var now = DateTime.UtcNow;

            var activeVipUserIds = await _context.Users
                .AsNoTracking()
                .Where(user => user.Status == VipStatus.Availlable)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);

            if (activeVipUserIds.Count == 0)
            {
                return;
            }

            var expirations = await _vipExpirationReadService.GetExpirationUtcByUserIdsAsync(
                activeVipUserIds,
                cancellationToken);

            var toDeactivateIds = activeVipUserIds
                .Where(userId => !expirations.TryGetValue(userId, out var expiresAt) || expiresAt <= now)
                .ToList();

            if (toDeactivateIds.Count == 0)
            {
                return;
            }

            await _context.Users
                .Where(user => toDeactivateIds.Contains(user.Id))
                .ExecuteUpdateAsync(
                    setter => setter.SetProperty(user => user.Status, VipStatus.Unavaillable),
                    cancellationToken);

            _logger.LogInformation("VIP снят у пользователей: {Count}", toDeactivateIds.Count);
        }
    }
}
