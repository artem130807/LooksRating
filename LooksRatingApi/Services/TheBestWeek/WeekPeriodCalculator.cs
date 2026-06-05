using System.Globalization;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Services.TheBestWeek
{
    internal static class WeekPeriodCalculator
    {
        public const int TopPhotoCount = 10;

        public static (int Year, int WeekOfYear, DateTime PeriodStart, DateTime PeriodEnd, WeekEnum WeekLabel) GetPreviousWeekPeriod(
            DateTime localNow)
        {
            var periodEnd = localNow.Date;
            var periodStart = periodEnd.AddDays(-7);
            var anchor = periodEnd.AddDays(-1);
            var year = ISOWeek.GetYear(anchor);
            var weekOfYear = ISOWeek.GetWeekOfYear(anchor);
            var weekLabel = MapWeekOfMonth(anchor);
            return (year, weekOfYear, periodStart, periodEnd, weekLabel);
        }

        private static WeekEnum MapWeekOfMonth(DateTime date)
        {
            var index = (date.Day - 1) / 7 + 1;
            return index switch
            {
                1 => WeekEnum.First,
                2 => WeekEnum.Second,
                3 => WeekEnum.Third,
                4 => WeekEnum.Fourth,
                _ => WeekEnum.Fifth
            };
        }
    }
}
