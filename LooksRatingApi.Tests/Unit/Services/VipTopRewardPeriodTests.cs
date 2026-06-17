using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class VipTopRewardPeriodTests
{
    [Fact]
    public void BuildKey_UsesSamePeriodForDatesWithin14Days()
    {
        var seasonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        // Period 56 spans 2026-02-23 .. 2026-03-08 (14-day buckets from 2024-01-01 epoch).
        var day1 = new DateTime(2026, 2, 23, 10, 0, 0);
        var day14 = new DateTime(2026, 3, 8, 23, 59, 0);

        var key1 = VipTopRewardPeriod.BuildKey(seasonId, day1);
        var key2 = VipTopRewardPeriod.BuildKey(seasonId, day14);

        key1.Should().Be(key2);
        key1.Should().Be($"{seasonId:N}:56");
    }

    [Fact]
    public void BuildKey_ChangesAfterRewardPeriod()
    {
        var seasonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var periodA = VipTopRewardPeriod.BuildKey(seasonId, new DateTime(2026, 3, 8));
        var periodB = VipTopRewardPeriod.BuildKey(seasonId, new DateTime(2026, 3, 9));

        periodB.Should().NotBe(periodA);
    }

    [Fact]
    public void BuildSparksPayload_IsStableForSameRecipient()
    {
        var periodKey = "season:42";
        var payload1 = VipTopRewardPeriod.BuildSparksPayload(periodKey, 3, 12345, "moscow-m-25");
        var payload2 = VipTopRewardPeriod.BuildSparksPayload(periodKey, 3, 12345, "moscow-m-25");

        payload1.Should().Be(payload2);
        payload1.Should().Contain("vip-sparks:");
    }
}
