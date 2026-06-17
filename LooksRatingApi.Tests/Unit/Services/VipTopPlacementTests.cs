using LooksRatingApi.Services;
using LooksRatingApi.Tests.Infrastructure.Builders;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class VipTopPlacementTests
{
    [Fact]
    public void GetExtensionTelegramIds_IncludesPlacesSixThroughTen()
    {
        var seasonId = Guid.NewGuid();
        var category = VipTopTestDataBuilder.CreateCategory(
            seasonId,
            telegramIds: [101, 102, 103, 104, 105, 106, 107, 108, 109, 110]);

        var telegramIds = VipTopPlacement.GetExtensionTelegramIds([category]);

        telegramIds.Should().BeEquivalentTo([106L, 107L, 108L, 109L, 110L]);
    }

    [Fact]
    public void GetExtensionTelegramIds_IgnoresNonPositiveTelegramIds()
    {
        var seasonId = Guid.NewGuid();
        var category = VipTopTestDataBuilder.CreateCategory(
            seasonId,
            telegramIds: [0, 0, 0, 0, 0, 201, 202, 203, 204, 205]);

        var telegramIds = VipTopPlacement.GetExtensionTelegramIds([category]);

        telegramIds.Should().BeEquivalentTo([201L, 202L, 203L, 204L, 205L]);
    }

    [Fact]
    public void GetSparksRewardRecipients_IncludesPlacesOneThroughFiveWithAmounts()
    {
        var seasonId = Guid.NewGuid();
        var category = VipTopTestDataBuilder.CreateCategory(
            seasonId,
            telegramIds: [301, 302, 303, 304, 305, 306, 307, 308, 309, 310]);

        var recipients = VipTopPlacement.GetSparksRewardRecipients([category]);

        recipients.Should().HaveCount(5);
        recipients.Select(recipient => recipient.TelegramId).Should().Equal(301L, 302L, 303L, 304L, 305L);
        recipients.Select(recipient => recipient.Place).Should().Equal(1, 2, 3, 4, 5);
        recipients.Select(recipient => recipient.SparksAmount).Should().Equal(4000m, 3000m, 2000m, 2000m, 2000m);
        recipients.Select(recipient => recipient.CategoryFingerprint).Distinct().Should().ContainSingle();
    }

    [Fact]
    public void BuildCategoryFingerprint_IsStableForSameCategory()
    {
        var seasonId = Guid.NewGuid();
        var category = VipTopTestDataBuilder.CreateCategory(seasonId, telegramIds: [401, 402, 403]);

        var fingerprint1 = VipTopPlacement.BuildCategoryFingerprint(category);
        var fingerprint2 = VipTopPlacement.BuildCategoryFingerprint(category);

        fingerprint1.Should().Be(fingerprint2);
        fingerprint1.Should().HaveLength(8);
    }
}
