using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Unit.Services.SeasonLifecycle;

public sealed class NewSeasonProcessorTests
{
    [Fact]
    public async Task ProcessMonthlyRolloverAsync_WhenNotFirstDay_DoesNothing()
    {
        var seasons = Substitute.For<ISeasonRepository>();
        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 2, 15)),
            photoProfiles: Substitute.For<IPhotoProfileRepository>(),
            seasons: seasons,
            lists: Substitute.For<IListSeasonsRepository>());

        await processor.ProcessMonthlyRolloverAsync(CancellationToken.None);

        await seasons.DidNotReceive().Create(Arg.Any<Season>());
    }

    [Fact]
    public async Task ProcessMonthlyRolloverAsync_WhenLockNotAcquired_DoesNothing()
    {
        var lists = Substitute.For<IListSeasonsRepository>();
        lists.GetLatest(includeSeasons: false).Returns(new ListSeasons { Id = Guid.NewGuid() });

        var lockService = CreateLockService(acquire: false);
        var seasons = Substitute.For<ISeasonRepository>();

        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 2, 1)),
            photoProfiles: Substitute.For<IPhotoProfileRepository>(),
            seasons: seasons,
            lists: lists,
            lockService: lockService);

        await processor.ProcessMonthlyRolloverAsync(CancellationToken.None);

        await seasons.DidNotReceive().Create(Arg.Any<Season>());
    }

    [Fact]
    public async Task ProcessMonthlyRolloverAsync_OnFirstDay_RewardsSeasonTopBeforeArchive()
    {
        var chapterId = Guid.NewGuid();
        var currentSeason = Season.Create("January", 1, chapterId).Value;
        var lists = Substitute.For<IListSeasonsRepository>();
        lists.GetLatest(includeSeasons: false).Returns(new ListSeasons { Id = chapterId });

        var seasons = Substitute.For<ISeasonRepository>();
        seasons.GetCurrentByList(chapterId).Returns(currentSeason);

        var photoProfiles = Substitute.For<IPhotoProfileRepository>();
        var profileIds = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToList();
        photoProfiles
            .GetProfileIdsBatchAsync(currentSeason.Id, 0, 5000, Arg.Any<CancellationToken>())
            .Returns(profileIds);
        photoProfiles
            .GetProfileIdsBatchAsync(currentSeason.Id, 5000, 5000, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var seasonRewards = Substitute.For<ISeasonTopSparksRewardProcessor>();
        seasonRewards
            .ProcessForSeasonAsync(currentSeason.Id, false, Arg.Any<CancellationToken>())
            .Returns(new SeasonTopSparksRewardResult(5, 0, 0, 0));

        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 2, 1)),
            photoProfiles: photoProfiles,
            seasons: seasons,
            lists: lists,
            seasonRewards: seasonRewards);

        await processor.ProcessMonthlyRolloverAsync(CancellationToken.None);

        Received.InOrder(async () =>
        {
            await seasonRewards.ProcessForSeasonAsync(currentSeason.Id, false, Arg.Any<CancellationToken>());
            await photoProfiles.ArchiveProfilesAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ProcessMonthlyRolloverAsync_OnFirstDay_ClosesSeasonArchivesAndCreatesNext()
    {
        var chapterId = Guid.NewGuid();
        var currentSeason = Season.Create("January", 1, chapterId).Value;
        var lists = Substitute.For<IListSeasonsRepository>();
        lists.GetLatest(includeSeasons: false).Returns(new ListSeasons { Id = chapterId });

        var seasons = Substitute.For<ISeasonRepository>();
        seasons.GetCurrentByList(chapterId).Returns(currentSeason);

        var profileIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
        var photoProfiles = Substitute.For<IPhotoProfileRepository>();
        photoProfiles
            .GetProfileIdsBatchAsync(currentSeason.Id, 0, 5000, Arg.Any<CancellationToken>())
            .Returns(profileIds);
        photoProfiles
            .GetProfileIdsBatchAsync(currentSeason.Id, 5000, 5000, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 2, 1)),
            photoProfiles: photoProfiles,
            seasons: seasons,
            lists: lists);

        await processor.ProcessMonthlyRolloverAsync(CancellationToken.None);

        await photoProfiles.Received(1).ArchiveProfilesAsync(
            Arg.Is<List<Guid>>(ids => ids.Count == 3),
            Arg.Any<CancellationToken>());
        currentSeason.IsClosed.Should().BeTrue();
        await seasons.Received(1).Update(currentSeason);
        await seasons.Received(1).Create(Arg.Is<Season>(season => season.Number == 2 && season.ListSeasonsId == chapterId));
    }

    [Fact]
    public async Task ProcessMonthlyRolloverAsync_WhenTargetSeasonAlreadyExists_SkipsCreation()
    {
        var chapterId = Guid.NewGuid();
        var lists = Substitute.For<IListSeasonsRepository>();
        lists.GetLatest(includeSeasons: false).Returns(new ListSeasons { Id = chapterId });

        var january = Season.Create("January", 1, chapterId).Value;
        var february = Season.Create("February", 2, chapterId).Value;
        var seasons = Substitute.For<ISeasonRepository>();
        seasons.GetCurrentByList(chapterId).Returns(january, february);

        var photoProfiles = Substitute.For<IPhotoProfileRepository>();
        photoProfiles
            .GetProfileIdsBatchAsync(january.Id, 0, 5000, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 2, 1)),
            photoProfiles: photoProfiles,
            seasons: seasons,
            lists: lists);

        await processor.ProcessMonthlyRolloverAsync(CancellationToken.None);

        await seasons.DidNotReceive().Create(Arg.Any<Season>());
    }

    [Fact]
    public async Task ProcessMonthlyRolloverAsync_WhenLockRenewFails_Throws()
    {
        var chapterId = Guid.NewGuid();
        var currentSeason = Season.Create("January", 1, chapterId).Value;
        var lists = Substitute.For<IListSeasonsRepository>();
        lists.GetLatest(includeSeasons: false).Returns(new ListSeasons { Id = chapterId });

        var seasons = Substitute.For<ISeasonRepository>();
        seasons.GetCurrentByList(chapterId).Returns(currentSeason);

        var fullBatch = Enumerable.Range(0, 5000).Select(_ => Guid.NewGuid()).ToList();
        var photoProfiles = Substitute.For<IPhotoProfileRepository>();
        photoProfiles
            .GetProfileIdsBatchAsync(currentSeason.Id, 0, 5000, Arg.Any<CancellationToken>())
            .Returns(fullBatch);
        photoProfiles
            .GetProfileIdsBatchAsync(currentSeason.Id, 5000, 5000, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { Guid.NewGuid() });

        var renewCalls = 0;
        var handle = new TestDistributedLockHandle(renew: (_, _) =>
        {
            renewCalls++;
            return Task.FromResult(renewCalls <= 1);
        });

        var lockService = CreateLockService(handle);

        var processor = CreateProcessor(
            clock: new FakeApplicationClock(new DateTime(2026, 2, 1)),
            photoProfiles: photoProfiles,
            seasons: seasons,
            lists: lists,
            lockService: lockService);

        var act = () => processor.ProcessMonthlyRolloverAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archive lock was lost*");
    }

    private static NewSeasonProcessor CreateProcessor(
        FakeApplicationClock clock,
        IPhotoProfileRepository photoProfiles,
        ISeasonRepository seasons,
        IListSeasonsRepository lists,
        ArchivingLockService? lockService = null,
        ISeasonTopSparksRewardProcessor? seasonRewards = null)
    {
        var loadingCities = Substitute.For<ILoadingCityService>();
        loadingCities.GetCityNames().Returns(new HashSet<string> { "Moscow" });

        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        var rewards = seasonRewards ?? CreateDefaultSeasonRewardsMock();

        return new NewSeasonProcessor(
            photoProfiles,
            seasons,
            lists,
            loadingCities,
            new NormalizeCityNameService(),
            lockService ?? CreateLockService(),
            rewards,
            clock,
            redis,
            NullLogger<NewSeasonProcessor>.Instance);
    }

    private static ArchivingLockService CreateLockService(
        bool acquire = true,
        IRedisDistributedLockHandle? handle = null)
    {
        var distributedLock = Substitute.For<IRedisDistributedLock>();
        if (acquire)
        {
            distributedLock
                .TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                .Returns(handle ?? new TestDistributedLockHandle());
        }
        else
        {
            distributedLock
                .TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                .Returns((IRedisDistributedLockHandle?)null);
        }

        return new ArchivingLockService(distributedLock);
    }

    private static ArchivingLockService CreateLockService(IRedisDistributedLockHandle handle) =>
        CreateLockService(acquire: true, handle: handle);

    private static ISeasonTopSparksRewardProcessor CreateDefaultSeasonRewardsMock()
    {
        var rewards = Substitute.For<ISeasonTopSparksRewardProcessor>();
        rewards
            .ProcessForSeasonAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new SeasonTopSparksRewardResult(0, 0, 0, 0));
        return rewards;
    }
}
