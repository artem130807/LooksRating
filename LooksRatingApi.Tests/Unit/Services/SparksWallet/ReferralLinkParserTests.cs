using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class ReferralLinkParserTests
{
    [Fact]
    public void TryParseReferrerUserId_ParsesRawGuid()
    {
        var userId = Guid.NewGuid();

        ReferralLinkParser.TryParseReferrerUserId(userId.ToString(), out var parsed).Should().BeTrue();
        parsed.Should().Be(userId);
    }

    [Fact]
    public void TryParseReferrerUserId_ParsesTelegramDeepLink()
    {
        var userId = Guid.NewGuid();
        var link = $"https://t.me/LooksRatingBot?start={userId}";

        ReferralLinkParser.TryParseReferrerUserId(link, out var parsed).Should().BeTrue();
        parsed.Should().Be(userId);
    }
}
