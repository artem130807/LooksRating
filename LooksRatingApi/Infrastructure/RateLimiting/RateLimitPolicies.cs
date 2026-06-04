namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public static class RateLimitPolicies
    {
        public const string Global = "Global";
        public const string GetNextPhoto = "GetNextPhoto";
        public const string Rating = "Rating";
        public const string Writes = "Writes";
        public const string Payments = "Payments";
        public const string Reads = "Reads";
        public const string AccountSensitive = "AccountSensitive";
        public const string Grpc = "Grpc";
    }
}
