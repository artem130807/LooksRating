using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.PaymentOrderContracts
{
    public interface IPaymentOrderRepository
    {
        Task CreateAsync(PaymentOrder paymentOrder, CancellationToken cancellationToken = default);
        Task<PaymentOrder?> GetByPayloadAsync(string payload, CancellationToken cancellationToken = default);
        Task<PaymentOrder?> GetByTelegramChargeIdAsync(string telegramPaymentChargeId, CancellationToken cancellationToken = default);
        Task<HashSet<string>> GetExistingPaidPayloadsAsync(
            IReadOnlyCollection<string> payloads,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
