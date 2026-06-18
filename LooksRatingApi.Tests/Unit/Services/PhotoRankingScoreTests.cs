using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class PhotoRankingScoreTests
{
    [Fact]
    public void Compare_WithEqualRatings_UsesCreatedAtThenId()
    {
        var older = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        var idLow = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var idHigh = Guid.Parse("00000000-0000-0000-0000-000000000002");

        PhotoRankingScore.Compare(
                8m,
                0,
                newer,
                idHigh,
                8m,
                0,
                older,
                idLow)
            .Should().BeLessThan(0);

        PhotoRankingScore.Compare(
                8m,
                0,
                older,
                idHigh,
                8m,
                0,
                older,
                idLow)
            .Should().BeGreaterThan(0);
    }
}
