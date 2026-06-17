using LooksRatingApi.Contracts;
using LooksRatingApi.Services;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class SeasonTopSparksRewardProcessorTests
{
    [Fact]
    public async Task ProcessForSeasonAsync_WhenNoQualifiedCategories_ReturnsZeros()
    {
        var categories = Substitute.For<ISeasonTopCategoryService>();
        categories
            .GetQualifiedCategoriesForSeasonAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VipTopCategory>());

        var processor = CreateProcessor(categories: categories);

        var result = await processor.ProcessForSeasonAsync(Guid.NewGuid(), seasonIsClosed: false, CancellationToken.None);

        result.Should().Be(new SeasonTopSparksRewardResult(0, 0, 0, 0));
    }

    [Fact]
    public async Task ProcessForSeasonAsync_DelegatesCreditingForQualifiedCategories()
    {
        var seasonId = Guid.NewGuid();
        var category = VipTopTestDataBuilder.CreateCategory(
            seasonId,
            telegramIds: [801, 802, 803, 804, 805, 806, 807, 808, 809, 810]);

        var categories = Substitute.For<ISeasonTopCategoryService>();
        categories
            .GetQualifiedCategoriesForSeasonAsync(seasonId, false, Arg.Any<CancellationToken>())
            .Returns([category]);

        var crediting = Substitute.For<ISparksRewardCreditingService>();
        crediting
            .CreditAsync(Arg.Any<IReadOnlyList<SparksRewardRecipient>>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SparksRewardCreditingResult(10, 0, 0, 0));

        var processor = CreateProcessor(categories: categories, crediting: crediting);

        var result = await processor.ProcessForSeasonAsync(seasonId, seasonIsClosed: false, CancellationToken.None);

        result.Should().Be(new SeasonTopSparksRewardResult(10, 0, 0, 0));
        await crediting.Received(1).CreditAsync(
            Arg.Is<IReadOnlyList<SparksRewardRecipient>>(recipients => recipients.Count == 10),
            SeasonTopRules.RewardProductCode,
            "season-top",
            Arg.Any<CancellationToken>());
    }

    private static SeasonTopSparksRewardProcessor CreateProcessor(
        ISeasonTopCategoryService? categories = null,
        ISparksRewardCreditingService? crediting = null)
    {
        return new SeasonTopSparksRewardProcessor(
            categories ?? Substitute.For<ISeasonTopCategoryService>(),
            crediting ?? Substitute.For<ISparksRewardCreditingService>(),
            NullLogger<SeasonTopSparksRewardProcessor>.Instance);
    }
}
