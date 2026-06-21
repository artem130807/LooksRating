using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services.SparksWallet;

/// <summary>
/// Decides how to reconcile an unresolved debit idempotency record
/// relative to its writing-off request (if any).
/// </summary>
public static class SparksDebitOrphanPolicy
{
    public enum OrphanResolution
    {
        None,
        CompensateAndMark,
        MarkOnly,
    }

    public static OrphanResolution GetResolution(
        SparksDebitIdempotency idempotency,
        WritingOffSparks? writingOff,
        string? pendingCreateIdempotencyKey)
    {
        if (idempotency.CompensatedAt is not null)
        {
            return OrphanResolution.None;
        }

        if (pendingCreateIdempotencyKey is not null
            && idempotency.IdempotencyKey == pendingCreateIdempotencyKey
            && writingOff is null)
        {
            return OrphanResolution.None;
        }

        if (writingOff is null)
        {
            return OrphanResolution.CompensateAndMark;
        }

        return writingOff.Status switch
        {
            OutputStatusEnum.Pending or OutputStatusEnum.Confirmed => OrphanResolution.None,
            _ => OrphanResolution.MarkOnly,
        };
    }
}
