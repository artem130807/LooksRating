namespace LooksRatingApi.Infrastructure.RateLimiting
{
    internal static class RateLimitEndpointPolicyResolver
    {
        public static IReadOnlyList<string> Resolve(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint is null)
            {
                return Array.Empty<string>();
            }

            return endpoint.Metadata
                .GetOrderedMetadata<RateLimitPolicyAttribute>()
                .Select(attribute => attribute.PolicyName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<string> BuildGrpcPolicyNames()
        {
            return
            [
                RateLimitPolicies.Global,
                RateLimitPolicies.Grpc,
            ];
        }

        public static IReadOnlyList<string> BuildRestPolicyNames(IReadOnlyList<string> endpointPolicies)
        {
            var policies = new List<string> { RateLimitPolicies.Global };
            if (endpointPolicies.Count > 0)
            {
                policies.AddRange(endpointPolicies);
            }

            return policies.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
