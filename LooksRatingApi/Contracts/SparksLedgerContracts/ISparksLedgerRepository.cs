using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface ISparksLedgerRepository
    {
        Task AddAsync(SparksWallet ledger, CancellationToken cancellationToken = default);

        Task<bool> ExistsByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<int> CountByUserAndTypeSinceAsync(
            Guid userId,
            SparksLedgerEnum type,
            DateTime sinceUtc,
            CancellationToken cancellationToken = default);

        Task<decimal> GetBalanceAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SparksWallet>> GetHistoryAsync(
            Guid userId,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<SparksWallet?> GetSparksByUserId(Guid userId, CancellationToken cancellationToken = default);

        Task<SparksWallet?> GetByAggregateIdAsync(Guid aggregateId, CancellationToken cancellationToken = default);

        Task UpdateBalanceAsync(Guid ledgerId, decimal newBalance, CancellationToken cancellationToken = default);
    }
}
