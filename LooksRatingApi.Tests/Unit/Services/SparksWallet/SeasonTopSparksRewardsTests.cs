using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class SeasonTopSparksRewardsTests
{
    [Theory]
    [InlineData(1, 800)]
    [InlineData(2, 600)]
    [InlineData(3, 500)]
    [InlineData(4, 400)]
    [InlineData(5, 400)]
    [InlineData(6, 300)]
    [InlineData(7, 300)]
    [InlineData(8, 200)]
    [InlineData(9, 200)]
    [InlineData(10, 200)]
    public void GetSparksForPlace_ReturnsConfiguredAmount(int place, decimal expected)
    {
        SeasonTopSparksRewards.GetSparksForPlace(place).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void GetSparksForPlace_OutsideRange_ReturnsZero(int place)
    {
        SeasonTopSparksRewards.GetSparksForPlace(place).Should().Be(0m);
    }
}
