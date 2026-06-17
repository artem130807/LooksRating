using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class SeasonTopRewardPeriodTests
{
    [Fact]
    public void BuildKey_IsStableForSeason()
    {
        var seasonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var key = SeasonTopRewardPeriod.BuildKey(seasonId);

        key.Should().Be($"{seasonId:N}:close");
    }

    [Fact]
    public void BuildSparksPayload_ContainsSeasonMarkerAndPlace()
    {
        var seasonId = Guid.NewGuid();
        var periodKey = SeasonTopRewardPeriod.BuildKey(seasonId);

        var payload = SeasonTopRewardPeriod.BuildSparksPayload(
            periodKey,
            place: 3,
            telegramId: 42,
            categoryFingerprint: "abcd1234");

        payload.Should().Be($"season-sparks:{periodKey}:3:42:abcd1234");
    }
}
