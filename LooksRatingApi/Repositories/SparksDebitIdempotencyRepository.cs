using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories;

public sealed class SparksDebitIdempotencyRepository : ISparksDebitIdempotencyRepository
{
    private readonly LooksRatingDbContext _context;

    public SparksDebitIdempotencyRepository(LooksRatingDbContext context)
    {
        _context = context;
    }

    public Task<SparksDebitIdempotency?> GetByUserIdAndIdempotencyKey(Guid userId, string idempotencyKey) =>
        _context.SparksDebitIdempotency
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IdempotencyKey == idempotencyKey);

    public async Task<IReadOnlyList<SparksDebitIdempotency>> GetUnresolvedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await _context.SparksDebitIdempotency
            .Where(x => x.UserId == userId && x.CompensatedAt == null)
            .ToListAsync(cancellationToken);

    public Task Add(SparksDebitIdempotency record)
    {
        _context.SparksDebitIdempotency.Add(record);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
