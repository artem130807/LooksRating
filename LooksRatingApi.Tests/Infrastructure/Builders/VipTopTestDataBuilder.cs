using LooksRatingApi.Contracts;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Tests.Infrastructure.Builders;

internal static class VipTopTestDataBuilder
{
    public static VipTopCategory CreateCategory(
        Guid seasonId,
        string city = "Moscow",
        params long[] telegramIds)
    {
        var profiles = telegramIds
            .Select((telegramId, index) => new VipTopProfileCandidate(
                telegramId,
                city,
                9.0m - index * 0.1m,
                10,
                25,
                GenderEnum.Male,
                DateTime.UtcNow))
            .ToList();

        return new VipTopCategory(seasonId, city, GenderEnum.Male, 25, profiles);
    }
}
