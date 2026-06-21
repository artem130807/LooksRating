using CSharpFunctionalExtensions;
using LooksRatingApi.Services;

namespace LooksRatingApi.Models;

public sealed class SparksDebitIdempotency
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public Guid DebitEventId { get; private set; }
    public decimal SparksAmount { get; private set; }
    public int StarsCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompensatedAt { get; private set; }

    private SparksDebitIdempotency()
    {
    }

    public static Result<SparksDebitIdempotency> Create(
        Guid userId,
        string idempotencyKey,
        Guid debitEventId,
        decimal sparksAmount,
        int starsCount)
    {
        if (!IdempotencyKeyService.TryNormalizeClientKey(idempotencyKey, out var normalizedKey))
        {
            return Result.Failure<SparksDebitIdempotency>("InvalidIdempotencyKey");
        }

        return new SparksDebitIdempotency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IdempotencyKey = normalizedKey,
            DebitEventId = debitEventId,
            SparksAmount = sparksAmount,
            StarsCount = starsCount,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void MarkCompensated() => CompensatedAt = DateTime.UtcNow;

    public void RenewAfterDebit(Guid debitEventId, decimal sparksAmount, int starsCount)
    {
        DebitEventId = debitEventId;
        SparksAmount = sparksAmount;
        StarsCount = starsCount;
        CompensatedAt = null;
    }
}
