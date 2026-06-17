using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public sealed class SparksLedgerRepository : ISparksLedgerRepository
    {
        private readonly LooksRatingDbContext _context;

        public SparksLedgerRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public Task AddAsync(SparksWallet ledger, CancellationToken cancellationToken = default)
        {
            return _context.SparksLedgers.AddAsync(ledger, cancellationToken).AsTask();
        }

        public Task<bool> ExistsByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var normalizedKey = idempotencyKey.Trim();
            return _context.SparksLedgers
                .AsNoTracking()
                .AnyAsync(x => x.IdempotencyKey == normalizedKey, cancellationToken);
        }

        public Task<int> CountByUserAndTypeSinceAsync(
            Guid userId,
            SparksLedgerEnum type,
            DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            return _context.SparksLedgers
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.CreatedAt >= sinceUtc)
                .CountAsync(cancellationToken);
        }

        public Task<decimal> GetBalanceAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return _context.SparksLedgers
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (decimal?)x.SparksCount)
                .SumAsync(x => x ?? 0m, cancellationToken);
        }

        public async Task<IReadOnlyList<SparksWallet>> GetHistoryAsync(
            Guid userId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            if (take <= 0)
            {
                return Array.Empty<SparksWallet>();
            }

            if (skip < 0)
            {
                skip = 0;
            }

            return await _context.SparksLedgers
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public Task<SparksWallet?> GetSparksByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            return _context.SparksLedgers.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        }

        public Task<SparksWallet?> GetByAggregateIdAsync(Guid aggregateId, CancellationToken cancellationToken = default)
        {
            return _context.SparksLedgers.FirstOrDefaultAsync(s => s.Id == aggregateId, cancellationToken);
        }

        public async Task UpdateBalanceAsync(
            Guid ledgerId,
            decimal newBalance,
            CancellationToken cancellationToken = default)
        {
            await _context.SparksLedgers
                .Where(x => x.Id == ledgerId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.SparksCount, newBalance),
                    cancellationToken);
        }
    }
}
