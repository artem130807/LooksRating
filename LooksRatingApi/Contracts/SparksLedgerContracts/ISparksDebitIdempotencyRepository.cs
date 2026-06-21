using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.SparksLedgerContracts;

public interface ISparksDebitIdempotencyRepository
{
    Task<SparksDebitIdempotency?> GetByUserIdAndIdempotencyKey(Guid userId, string idempotencyKey);
    Task<IReadOnlyList<SparksDebitIdempotency>> GetUnresolvedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task Add(SparksDebitIdempotency record);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
