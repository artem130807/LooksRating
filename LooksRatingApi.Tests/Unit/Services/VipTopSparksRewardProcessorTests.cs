using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Services;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class VipTopSparksRewardProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenLockNotAcquired_ReturnsZeros()
    {
        var processor = CreateProcessor(
            distributedLock: CreateDistributedLock(acquire: false));

        var result = await processor.ProcessAsync(CancellationToken.None);

        result.Should().Be(new VipTopSparksRewardResult(0, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task ProcessAsync_WhenNoQualifiedCategories_ReturnsZeros()
    {
        var categories = Substitute.For<IVipTopCategoryService>();
        categories.GetQualifiedCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VipTopCategory>());

        var processor = CreateProcessor(categories: categories);

        var result = await processor.ProcessAsync(CancellationToken.None);

        result.Should().Be(new VipTopSparksRewardResult(0, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task ProcessAsync_WhenCategoriesSpanMultipleSeasons_ReturnsZeros()
    {
        var seasonA = Guid.NewGuid();
        var seasonB = Guid.NewGuid();
        var categories = Substitute.For<IVipTopCategoryService>();
        categories.GetQualifiedCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns([
                VipTopTestDataBuilder.CreateCategory(seasonA, telegramIds: [501, 502, 503]),
                VipTopTestDataBuilder.CreateCategory(seasonB, telegramIds: [601, 602, 603]),
            ]);

        var processor = CreateProcessor(categories: categories);

        var result = await processor.ProcessAsync(CancellationToken.None);

        result.Should().Be(new VipTopSparksRewardResult(0, 0, 0, 0, 0, 0, 0));
    }

    private static VipTopSparksRewardProcessor CreateProcessor(
        IVipTopCategoryService? categories = null,
        IRedisDistributedLock? distributedLock = null)
    {
        var categoryService = categories ?? Substitute.For<IVipTopCategoryService>();
        var extensionService = Substitute.For<IVipStatusExtensionService>();

        return new VipTopSparksRewardProcessor(
            categoryService,
            extensionService,
            Substitute.For<ISparksRewardCreditingService>(),
            distributedLock ?? CreateDistributedLock(),
            new FakeApplicationClock(new DateTime(2026, 3, 1)),
            NullLogger<VipTopSparksRewardProcessor>.Instance);
    }

    private static IRedisDistributedLock CreateDistributedLock(bool acquire = true)
    {
        var distributedLock = Substitute.For<IRedisDistributedLock>();
        distributedLock
            .TryAcquireAsync(
                DistributedLockKeys.VipTopSparksReward,
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(acquire ? new TestDistributedLockHandle() : null);

        return distributedLock;
    }
}
