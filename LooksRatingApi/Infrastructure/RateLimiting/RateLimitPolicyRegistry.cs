namespace LooksRatingApi.Infrastructure.RateLimiting
{
    internal static class RateLimitPolicyRegistry
    {
        public static IReadOnlyList<string> AllRequired { get; } =
        [
            RateLimitPolicies.Global,
            RateLimitPolicies.GetNextPhoto,
            RateLimitPolicies.Rating,
            RateLimitPolicies.Writes,
            RateLimitPolicies.Payments,
            RateLimitPolicies.Reads,
            RateLimitPolicies.AccountSensitive,
            RateLimitPolicies.Grpc,
        ];

        public static bool UsesTelegramPartition(string policyName)
        {
            return !string.Equals(policyName, RateLimitPolicies.Global, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(policyName, RateLimitPolicies.Grpc, StringComparison.OrdinalIgnoreCase);
        }
    }
}
