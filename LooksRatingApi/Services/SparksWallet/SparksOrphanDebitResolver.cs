using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.WritingOffSparks;

namespace LooksRatingApi.Services.SparksWallet;

public sealed class SparksOrphanDebitResolver : ISparksOrphanDebitResolver
{
    private const string CompensationReason = "orphan_debit_sweep";

    private readonly ISparksDebitIdempotencyRepository _sparksDebitIdempotencyRepository;
    private readonly IWritingOffSparksRepository _writingOffSparksRepository;
    private readonly ICurrencyDebitCompensatedService _currencyDebitCompensatedService;
    private readonly ISparksLedgerRepository _sparksLedgerRepository;
    private readonly LooksRatingDbContext _context;
    private readonly ILogger<SparksOrphanDebitResolver> _logger;

    public SparksOrphanDebitResolver(
        ISparksDebitIdempotencyRepository sparksDebitIdempotencyRepository,
        IWritingOffSparksRepository writingOffSparksRepository,
        ICurrencyDebitCompensatedService currencyDebitCompensatedService,
        ISparksLedgerRepository sparksLedgerRepository,
        LooksRatingDbContext context,
        ILogger<SparksOrphanDebitResolver> logger)
    {
        _sparksDebitIdempotencyRepository = sparksDebitIdempotencyRepository;
        _writingOffSparksRepository = writingOffSparksRepository;
        _currencyDebitCompensatedService = currencyDebitCompensatedService;
        _sparksLedgerRepository = sparksLedgerRepository;
        _context = context;
        _logger = logger;
    }

    public async Task ResolveOrphansAsync(
        Guid userId,
        string? pendingCreateIdempotencyKey,
        CancellationToken cancellationToken)
    {
        var unresolved = await _sparksDebitIdempotencyRepository.GetUnresolvedByUserIdAsync(
            userId,
            cancellationToken);
        if (unresolved.Count == 0)
        {
            return;
        }

        var resolutions = new List<(Models.SparksDebitIdempotency Record, SparksDebitOrphanPolicy.OrphanResolution Action)>();
        foreach (var record in unresolved)
        {
            var writingOff = await _writingOffSparksRepository.GetByUserIdAndIdempotencyKey(
                userId,
                record.IdempotencyKey);
            var action = SparksDebitOrphanPolicy.GetResolution(
                record,
                writingOff,
                pendingCreateIdempotencyKey);
            if (action != SparksDebitOrphanPolicy.OrphanResolution.None)
            {
                resolutions.Add((record, action));
            }
        }

        if (resolutions.Count == 0)
        {
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var (record, action) in resolutions)
            {
                if (action == SparksDebitOrphanPolicy.OrphanResolution.CompensateAndMark)
                {
                    await _currencyDebitCompensatedService.Compensate(
                        userId,
                        record.SparksAmount,
                        record.DebitEventId,
                        CompensationReason,
                        cancellationToken);
                }

                record.MarkCompensated();
            }

            await _sparksLedgerRepository.SaveChangesAsync(cancellationToken);
            await _sparksDebitIdempotencyRepository.SaveChangesAsync(cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Resolved {Count} orphan debit idempotency records for userId={UserId}",
                resolutions.Count,
                userId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to resolve orphan debits for userId={UserId}", userId);
            throw;
        }
    }
}
