namespace LooksRatingApi.Infrastructure.Quartz
{
    public static class ApplicationTimeZoneResolver
    {
        public const string DefaultTimeZoneId = "Europe/Moscow";

        private static readonly string[] MoscowFallbackIds =
        [
            "Europe/Moscow",
            "Russian Standard Time"
        ];

        public static TimeZoneInfo Resolve(string? timeZoneId)
        {
            var candidates = string.IsNullOrWhiteSpace(timeZoneId)
                ? MoscowFallbackIds
                : [timeZoneId, ..MoscowFallbackIds.Where(id => !id.Equals(timeZoneId, StringComparison.OrdinalIgnoreCase))];

            foreach (var candidate in candidates)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(candidate);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            throw new TimeZoneNotFoundException(
                $"Time zone '{timeZoneId ?? DefaultTimeZoneId}' was not found on this host.");
        }
    }
}
