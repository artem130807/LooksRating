namespace LooksRatingApi.Services
{
    /// <summary>
    /// Biweekly VIP-top reward calendar aligned with <see cref="VipTopRewardPeriod"/>.
    /// </summary>
    internal static class VipTopRewardSchedule
    {
        internal static readonly DateTime EpochDate = new(2024, 1, 1);

        public static int GetDaysSinceEpoch(DateTime applicationLocalDate) =>
            (int)(applicationLocalDate.Date - EpochDate).TotalDays;

        public static bool IsRewardDay(DateTime applicationLocalNow)
        {
            var days = GetDaysSinceEpoch(applicationLocalNow);
            return days >= 0 && days % VipTopRules.RewardPeriodDays == 0;
        }

        public static DateTime GetNextRewardDay(DateTime applicationLocalNow)
        {
            var days = GetDaysSinceEpoch(applicationLocalNow);
            if (days < 0)
                return EpochDate;

            var remainder = days % VipTopRules.RewardPeriodDays;
            if (remainder == 0)
                return applicationLocalNow.Date;

            return applicationLocalNow.Date.AddDays(VipTopRules.RewardPeriodDays - remainder);
        }

        public static DateTimeOffset GetFirstFireTime(TimeZoneInfo scheduleTimeZone, int hour, int minute)
        {
            var local = new DateTime(
                EpochDate.Year,
                EpochDate.Month,
                EpochDate.Day,
                hour,
                minute,
                0,
                DateTimeKind.Unspecified);
            var offset = scheduleTimeZone.GetUtcOffset(local);
            return new DateTimeOffset(local, offset);
        }
    }
}
