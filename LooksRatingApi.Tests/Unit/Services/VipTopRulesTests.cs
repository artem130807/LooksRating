using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class VipTopRulesTests
{
    [Fact]
    public void VipStarsPrice_Is140()
    {
        VipTopRules.VipStarsPrice.Should().Be(140);
    }

    [Fact]
    public void VipProductCode_IsStable()
    {
        VipTopRules.VipProductCode.Should().Be(1001);
    }
}
