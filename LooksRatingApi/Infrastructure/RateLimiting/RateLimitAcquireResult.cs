namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitAcquireResult
    {
        public static RateLimitAcquireResult Allowed { get; } = new(true, 0);

        public RateLimitAcquireResult(bool isAcquired, int retryAfterSeconds)
        {
            IsAcquired = isAcquired;
            RetryAfterSeconds = Math.Max(0, retryAfterSeconds);
        }

        public bool IsAcquired { get; }
        public int RetryAfterSeconds { get; }
    }
}
