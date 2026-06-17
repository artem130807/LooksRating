using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services.SeasonLifecycle;

public sealed class NewListSeasonProcessorTests
{
    [Fact]
    public async Task TryCreateNewChapterAsync_WhenNotJanuaryFirst_ReturnsFalse()
    {
        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 6, 1)),
            lists: Substitute.For<IListSeasonsRepository>(),
            seasons: Substitute.For<ISeasonRepository>());

        var created = await processor.TryCreateNewChapterAsync(CancellationToken.None);

        created.Should().BeFalse();
    }

    [Fact]
    public async Task TryCreateNewChapterAsync_WhenCurrentSeasonIsNotTwelfth_ReturnsFalse()
    {
        var chapterId = Guid.NewGuid();
        var lists = Substitute.For<IListSeasonsRepository>();
        lists.GetLatest(includeSeasons: false).Returns(new ListSeasons { Id = chapterId });

        var seasons = Substitute.For<ISeasonRepository>();
        seasons.GetCurrentByList(chapterId).Returns(Season.Create("November", 11, chapterId).Value);

        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 1, 1)),
            lists: lists,
            seasons: seasons);

        var created = await processor.TryCreateNewChapterAsync(CancellationToken.None);

        created.Should().BeFalse();
        await lists.DidNotReceive().Create(Arg.Any<ListSeasons>());
    }

    [Fact]
    public async Task TryCreateNewChapterAsync_OnJanuaryFirstWithSeasonTwelve_CreatesChapter()
    {
        var chapterId = Guid.NewGuid();
        var lists = Substitute.For<IListSeasonsRepository>();
        lists.GetLatest(includeSeasons: false).Returns(new ListSeasons { Id = chapterId });

        var seasons = Substitute.For<ISeasonRepository>();
        seasons.GetCurrentByList(chapterId).Returns(Season.Create("December", 12, chapterId).Value);

        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 1, 1)),
            lists: lists,
            seasons: seasons);

        var created = await processor.TryCreateNewChapterAsync(CancellationToken.None);

        created.Should().BeTrue();
        await lists.Received(1).Create(Arg.Any<ListSeasons>());
    }

    private static NewListSeasonProcessor CreateProcessor(
        FakeApplicationClock clock,
        IListSeasonsRepository lists,
        ISeasonRepository seasons,
        bool acquireLock = true)
    {
        var distributedLock = Substitute.For<IRedisDistributedLock>();
        distributedLock
            .TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(acquireLock ? new TestDistributedLockHandle() : null);

        return new NewListSeasonProcessor(
            lists,
            seasons,
            new ArchivingLockService(distributedLock),
            clock,
            NullLogger<NewListSeasonProcessor>.Instance);
    }
}
