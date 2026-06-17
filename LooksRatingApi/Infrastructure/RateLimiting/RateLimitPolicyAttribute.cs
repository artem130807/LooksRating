namespace LooksRatingApi.Infrastructure.RateLimiting
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class RateLimitPolicyAttribute : Attribute
    {
        public RateLimitPolicyAttribute(string policyName)
        {
            PolicyName = policyName;
        }

        public string PolicyName { get; }
    }
}
