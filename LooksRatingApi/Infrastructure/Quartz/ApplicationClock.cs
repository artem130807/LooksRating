namespace LooksRatingApi.Infrastructure.Quartz
{
    public class ApplicationClock
    {
        public ApplicationClock(TimeZoneInfo timeZone)
        {
            TimeZone = timeZone;
        }

        public TimeZoneInfo TimeZone { get; }

        public virtual DateTime GetNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);
    }
}
