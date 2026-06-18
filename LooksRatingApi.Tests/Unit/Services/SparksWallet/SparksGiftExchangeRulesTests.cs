using FluentAssertions;
using LooksRatingApi.CQRS.Payments.Query.GetGiftExchangeRates;
using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class SparksGiftExchangeRulesTests
{
    [Theory]
    [InlineData(100, 1200)]
    [InlineData(200, 2400)]
    [InlineData(300, 3600)]
    [InlineData(400, 4800)]
    public void TryGetSparksCost_ReturnsTwelveSparksPerStar(int starTier, decimal expectedSparks)
    {
        var success = SparksGiftExchangeRules.TryGetSparksCost(starTier, out var sparksCost);

        success.Should().BeTrue();
        sparksCost.Should().Be(expectedSparks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(150)]
    [InlineData(500)]
    public void TryGetSparksCost_RejectsUnknownTier(int starTier)
    {
        var success = SparksGiftExchangeRules.TryGetSparksCost(starTier, out var sparksCost);

        success.Should().BeFalse();
        sparksCost.Should().Be(0m);
    }

    [Fact]
    public void GetRates_ExposesAllAllowedTiers()
    {
        var rates = SparksGiftExchangeRules.GetRates();

        rates.Should().HaveCount(4);
        rates.Select(rate => rate.StarTier).Should().Equal(SparksGiftExchangeRules.AllowedStarTiers);
        rates.Should().OnlyContain(rate => rate.SparksCost == rate.StarTier * SparksGiftExchangeRules.SparksPerStar);
    }
}

public sealed class GetGiftExchangeRatesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCanonicalRates()
    {
        var handler = new GetGiftExchangeRatesHandler();

        var result = await handler.Handle(new GetGiftExchangeRatesQuery(), CancellationToken.None);

        result.SparksPerStar.Should().Be(12);
        result.Gifts.Should().HaveCount(4);
        result.Gifts.Should().Contain(item => item.StarTier == 100 && item.SparksCost == 1200);
        result.Gifts.Should().Contain(item => item.StarTier == 400 && item.SparksCost == 4800);
    }
}
