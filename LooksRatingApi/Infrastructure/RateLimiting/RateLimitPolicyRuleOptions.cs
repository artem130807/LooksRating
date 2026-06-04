namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitPolicyRuleOptions
    {
        public int PermitLimit { get; set; }
        public int WindowSeconds { get; set; } = 60;
        public int? BurstPermitLimit { get; set; }
        public int? BurstWindowSeconds { get; set; }
    }
}
