using LooksRatingApi.Enums;
using LooksRatingApi.Services.TheBestWeek;

namespace LooksRatingApi.Tests.Unit.Services.TheBestWeek;

public sealed class TheBestWeekTopTelegramIdsCollectorTests
{
    [Fact]
    public void CollectForCity_ReturnsDistinctTelegramIdsAcrossAgeBrackets()
    {
        var items = new List<TheBestWeekSnapshotItem>
        {
            CreateItem(telegramId: 1001, city: "moscow", age: 18, gender: GenderEnum.Male, rating: 9.5m, ratingCount: 20),
            CreateItem(telegramId: 1001, city: "moscow", age: 19, gender: GenderEnum.Male, rating: 9.0m, ratingCount: 15),
            CreateItem(telegramId: 1002, city: "moscow", age: 18, gender: GenderEnum.Male, rating: 8.0m, ratingCount: 10),
        };

        var ids = TheBestWeekTopTelegramIdsCollector.CollectForCity("moscow", items);

        ids.Should().BeEquivalentTo([1001L, 1002L]);
    }

    [Fact]
    public void CollectForWeekRecords_AggregatesCitiesForSameWeek()
    {
        var weekRecords = new List<TheBestWeekWeekRecord>
        {
            new(
                "moscow",
                [CreateItem(1001, "moscow", 18, GenderEnum.Male, 9.5m, 20)]),
            new(
                "spb",
                [CreateItem(2002, "spb", 25, GenderEnum.Female, 9.8m, 30)]),
        };

        var ids = TheBestWeekTopTelegramIdsCollector.CollectForWeekRecords(weekRecords);

        ids.Should().BeEquivalentTo([1001L, 2002L]);
    }

    private static TheBestWeekSnapshotItem CreateItem(
        long telegramId,
        string city,
        int age,
        GenderEnum gender,
        decimal rating,
        int ratingCount)
    {
        return new TheBestWeekSnapshotItem
        {
            ProfileId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TelegramId = telegramId,
            City = city,
            AgeNomination = age,
            GenderNomination = gender,
            Rating = rating,
            RatingCount = ratingCount,
            CreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        };
    }
}
