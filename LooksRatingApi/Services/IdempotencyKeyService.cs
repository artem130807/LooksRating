namespace LooksRatingApi.Services;

public static class IdempotencyKeyService
{
    public const int MaxLength = 128;

    public static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        return idempotencyKey.Trim();
    }

    public static bool TryNormalizeClientKey(string? idempotencyKey, out string normalizedKey)
    {
        normalizedKey = NormalizeIdempotencyKey(idempotencyKey) ?? string.Empty;
        if (string.IsNullOrEmpty(normalizedKey) || normalizedKey.Length > MaxLength)
        {
            normalizedKey = string.Empty;
            return false;
        }

        return true;
    }
}
