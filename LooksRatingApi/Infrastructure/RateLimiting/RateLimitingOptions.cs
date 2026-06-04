namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        public bool Enabled { get; set; } = true;
        public bool FailOpen { get; set; } = true;
        public string KeyPrefix { get; set; } = "looksrating:rl";
        public Dictionary<string, RateLimitPolicyRuleOptions> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
