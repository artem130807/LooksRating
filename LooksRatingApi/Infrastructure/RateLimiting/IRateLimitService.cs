namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public interface IRateLimitService
    {
        Task<RateLimitAcquireResult> TryAcquireAsync(
            string policyName,
            string partitionKey,
            CancellationToken cancellationToken = default);
    }
}
