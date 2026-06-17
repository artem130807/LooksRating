using LooksRatingApi.Contracts;
using LooksRatingApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services
{
    public sealed class VipExpirationReadService : IVipExpirationReadService
    {
        private readonly LooksRatingDbContext _context;

        public VipExpirationReadService(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyDictionary<Guid, DateTime>> GetExpirationUtcByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, DateTime>();
            }

            var expirationRows = await _context.PaymentOrders
                .AsNoTracking()
                .Where(order =>
                    userIds.Contains(order.UserId)
                    && order.Status == PaymentOrderStatus.Paid
                    && order.PaidAt.HasValue)
                .Select(order => new
                {
                    order.UserId,
                    ExpiresAt = order.PaidAt!.Value.AddDays(
                        order.Product.VipDays > 0 ? order.Product.VipDays : VipTopRules.DefaultVipDays),
                })
                .GroupBy(row => row.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    ExpiresAt = group.Max(row => row.ExpiresAt),
                })
                .ToListAsync(cancellationToken);

            return expirationRows.ToDictionary(row => row.UserId, row => row.ExpiresAt);
        }
    }
}
