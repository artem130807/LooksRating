using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Services.TheBestWeek;

namespace LooksRatingApi.Tests.Unit.Services.TheBestWeek;

public sealed class TheBestWeekTopStatsServiceTests
{
    [Fact]
    public async Task CountWeekAppearancesForTelegramIdAsync_CountsDistinctWeeksOnly()
    {
        var repository = Substitute.For<ITheBestWeekRepository>();
        repository
            .GetAllWeekSnapshotRecordsGroupedAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                [
                    new TheBestWeekWeekRecord(
                        "moscow",
                        [
                            new TheBestWeekSnapshotItem
                            {
                                TelegramId = 1001,
                                City = "moscow",
                                AgeNomination = 18,
                                GenderNomination = Enums.GenderEnum.Male,
                                Rating = 9.5m,
                                RatingCount = 20,
                                CreatedAt = DateTime.UtcNow,
                            },
                        ]),
                ],
                [
                    new TheBestWeekWeekRecord(
                        "moscow",
                        [
                            new TheBestWeekSnapshotItem
                            {
                                TelegramId = 1001,
                                City = "moscow",
                                AgeNomination = 18,
                                GenderNomination = Enums.GenderEnum.Male,
                                Rating = 9.0m,
                                RatingCount = 15,
                                CreatedAt = DateTime.UtcNow,
                            },
                        ]),
                ],
            ]);

        var service = new TheBestWeekTopStatsService(repository);

        var count = await service.CountWeekAppearancesForTelegramIdAsync(1001, CancellationToken.None);

        count.Should().Be(2);
    }

    [Fact]
    public async Task CountWeekAppearancesForTelegramIdAsync_ReturnsZeroWhenUserNeverInTop()
    {
        var repository = Substitute.For<ITheBestWeekRepository>();
        repository
            .GetAllWeekSnapshotRecordsGroupedAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var service = new TheBestWeekTopStatsService(repository);

        var count = await service.CountWeekAppearancesForTelegramIdAsync(1001, CancellationToken.None);

        count.Should().Be(0);
    }
}
