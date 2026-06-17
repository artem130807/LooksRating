using LooksRatingApi.Services;
using LooksRatingApi.Tests.Infrastructure.Builders;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class SeasonTopPlacementTests
{
    [Fact]
    public void GetSparksRewardRecipients_IncludesPlacesOneThroughTenWithAmounts()
    {
        var seasonId = Guid.NewGuid();
        var category = VipTopTestDataBuilder.CreateCategory(
            seasonId,
            telegramIds: [101, 102, 103, 104, 105, 106, 107, 108, 109, 110]);

        var recipients = SeasonTopPlacement.GetSparksRewardRecipients([category]);

        recipients.Should().HaveCount(10);
        recipients.Select(recipient => recipient.TelegramId)
            .Should().Equal(101L, 102L, 103L, 104L, 105L, 106L, 107L, 108L, 109L, 110L);
        recipients.Select(recipient => recipient.SparksAmount)
            .Should().Equal(800m, 600m, 500m, 400m, 400m, 300m, 300m, 200m, 200m, 200m);
        recipients.Select(recipient => recipient.CategoryFingerprint).Distinct().Should().ContainSingle();
    }

    [Fact]
    public void GetSparksRewardRecipients_IgnoresNonPositiveTelegramIds()
    {
        var seasonId = Guid.NewGuid();
        var category = VipTopTestDataBuilder.CreateCategory(
            seasonId,
            telegramIds: [0, 0, 0, 0, 0, 0, 0, 0, 201, 202]);

        var recipients = SeasonTopPlacement.GetSparksRewardRecipients([category]);

        recipients.Should().HaveCount(2);
        recipients.Select(recipient => recipient.TelegramId).Should().Equal(201L, 202L);
        recipients.Select(recipient => recipient.Place).Should().Equal(9, 10);
    }
}
