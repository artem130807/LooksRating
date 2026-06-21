namespace LooksRatingApi.Contracts.SparksLedgerContracts;

/// <summary>
/// Reconciles stale debit idempotency records before a new sparks debit.
/// </summary>
public interface ISparksOrphanDebitResolver
{
    /// <param name="pendingCreateIdempotencyKey">
    /// Current operation key that already debited but has not created a writing-off yet.
    /// Such a record must not be compensated.
    /// </param>
    Task ResolveOrphansAsync(
        Guid userId,
        string? pendingCreateIdempotencyKey,
        CancellationToken cancellationToken);
}
