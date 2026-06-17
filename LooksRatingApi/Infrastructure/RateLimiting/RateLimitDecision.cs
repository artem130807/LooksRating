namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitDecision
    {
        public static RateLimitDecision Allowed { get; } = new(true, null, 0);

        public RateLimitDecision(bool isAllowed, string? policy, int retryAfterSeconds)
        {
            IsAllowed = isAllowed;
            Policy = policy;
            RetryAfterSeconds = retryAfterSeconds;
        }

        public bool IsAllowed { get; }
        public string? Policy { get; }
        public int RetryAfterSeconds { get; }
    }
}
