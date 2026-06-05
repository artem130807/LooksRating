namespace LooksRatingApi.Infrastructure.Quartz
{
    public sealed class ApplicationClock
    {
        public ApplicationClock(TimeZoneInfo timeZone)
        {
            TimeZone = timeZone;
        }

        public TimeZoneInfo TimeZone { get; }

        public DateTime GetNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);
    }
}
