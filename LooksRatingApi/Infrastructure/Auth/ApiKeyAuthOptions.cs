namespace LooksRatingApi.Infrastructure.Auth
{
    public sealed class ApiKeyAuthOptions
    {
        public const string SectionName = "Security";

        public string ApiKey { get; set; } = string.Empty;
        public bool RequireApiKey { get; set; } = true;
        public string HeaderName { get; set; } = "X-Api-Key";
    }
}
