using System.Globalization;
using LooksRatingApi.Enums;
using LooksRatingApi.Services.TheBestWeek;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class WeekPeriodCalculatorTests
{
    [Fact]
    public void GetPreviousWeekPeriod_ReturnsSevenDayWindowEndingToday()
    {
        var now = new DateTime(2026, 6, 1, 12, 0, 0);
        var anchor = now.Date.AddDays(-1);

        var period = WeekPeriodCalculator.GetPreviousWeekPeriod(now);

        period.PeriodEnd.Should().Be(now.Date);
        period.PeriodStart.Should().Be(now.Date.AddDays(-7));
        period.Year.Should().Be(ISOWeek.GetYear(anchor));
        period.WeekOfYear.Should().Be(ISOWeek.GetWeekOfYear(anchor));
        period.WeekLabel.Should().Be(WeekEnum.Fifth);
    }
}
