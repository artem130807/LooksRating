using LooksRatingApi.Infrastructure.Quartz;
using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class VipTopRewardScheduleTests
{
    [Theory]
    [InlineData(2024, 1, 1)]
    [InlineData(2024, 1, 15)]
    [InlineData(2026, 3, 9)]
    public void IsRewardDay_ReturnsTrueOnPeriodBoundaries(int year, int month, int day)
    {
        var date = new DateTime(year, month, day, 10, 0, 0);

        VipTopRewardSchedule.IsRewardDay(date).Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 3, 1)]
    [InlineData(2026, 3, 8)]
    [InlineData(2024, 1, 14)]
    public void IsRewardDay_ReturnsFalseBetweenPeriodBoundaries(int year, int month, int day)
    {
        var date = new DateTime(year, month, day, 10, 0, 0);

        VipTopRewardSchedule.IsRewardDay(date).Should().BeFalse();
    }

    [Fact]
    public void GetNextRewardDay_ReturnsSameDateWhenAlreadyRewardDay()
    {
        var rewardDay = new DateTime(2026, 3, 9, 15, 30, 0);

        VipTopRewardSchedule.GetNextRewardDay(rewardDay).Should().Be(rewardDay.Date);
    }

    [Fact]
    public void GetNextRewardDay_ReturnsNextBoundaryWhenBetweenPeriods()
    {
        var betweenPeriods = new DateTime(2026, 3, 1, 10, 0, 0);

        VipTopRewardSchedule.GetNextRewardDay(betweenPeriods).Should().Be(new DateTime(2026, 3, 9));
    }

    [Fact]
    public void GetFirstFireTime_UsesEpochAtConfiguredLocalTime()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(ApplicationTimeZoneResolver.DefaultTimeZoneId);

        var firstFire = VipTopRewardSchedule.GetFirstFireTime(timeZone, hour: 10, minute: 0);

        firstFire.DateTime.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0));
        firstFire.Offset.Should().Be(timeZone.GetUtcOffset(firstFire.DateTime));
    }

    [Fact]
    public void RewardPeriodDays_MatchesScheduleInterval()
    {
        VipTopRules.RewardPeriodDays.Should().Be(14);
    }
}
