using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
    {
        public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.KeyPrefix))
            {
                return ValidateOptionsResult.Fail("RateLimiting:KeyPrefix is required.");
            }

            var errors = new List<string>();

            foreach (var policyName in RateLimitPolicyRegistry.AllRequired)
            {
                if (!options.Policies.TryGetValue(policyName, out var policy))
                {
                    errors.Add($"RateLimiting:Policies:{policyName} is not configured.");
                    continue;
                }

                if (policy.PermitLimit <= 0)
                {
                    errors.Add($"RateLimiting:Policies:{policyName}:PermitLimit must be greater than zero.");
                }

                if (policy.WindowSeconds <= 0)
                {
                    errors.Add($"RateLimiting:Policies:{policyName}:WindowSeconds must be greater than zero.");
                }

                if (policy.BurstPermitLimit is <= 0 || policy.BurstWindowSeconds is <= 0)
                {
                    if (policy.BurstPermitLimit.HasValue ^ policy.BurstWindowSeconds.HasValue)
                    {
                        errors.Add($"RateLimiting:Policies:{policyName} burst settings must define both BurstPermitLimit and BurstWindowSeconds.");
                    }
                }
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
