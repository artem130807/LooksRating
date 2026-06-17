using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public sealed class PaymentOrderRepository : IPaymentOrderRepository
    {
        private readonly LooksRatingDbContext _context;

        public PaymentOrderRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(PaymentOrder paymentOrder, CancellationToken cancellationToken = default)
        {
            await _context.PaymentOrders.AddAsync(paymentOrder, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<PaymentOrder?> GetByPayloadAsync(string payload, CancellationToken cancellationToken = default)
        {
            return _context.PaymentOrders
                .Include(x => x.User)
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Payload == payload, cancellationToken);
        }

        public Task<PaymentOrder?> GetByTelegramChargeIdAsync(string telegramPaymentChargeId, CancellationToken cancellationToken = default)
        {
            return _context.PaymentOrders
                .Include(x => x.User)
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.TelegramPaymentChargeId == telegramPaymentChargeId, cancellationToken);
        }

        public async Task<HashSet<string>> GetExistingPaidPayloadsAsync(
            IReadOnlyCollection<string> payloads,
            CancellationToken cancellationToken = default)
        {
            if (payloads.Count == 0)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var existing = await _context.PaymentOrders
                .AsNoTracking()
                .Where(order => payloads.Contains(order.Payload) && order.Status == PaymentOrderStatus.Paid)
                .Select(order => order.Payload)
                .ToListAsync(cancellationToken);

            return existing.ToHashSet(StringComparer.Ordinal);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
