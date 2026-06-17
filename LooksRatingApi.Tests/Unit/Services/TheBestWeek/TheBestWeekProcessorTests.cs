using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Services.TheBestWeek;
using LooksRatingApi.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Unit.Services.TheBestWeek;

public sealed class TheBestWeekProcessorTests
{
    [Fact]
    public async Task RefreshWeeklyAsync_WhenArchivingInProgress_SkipsRefresh()
    {
        var repository = Substitute.For<ITheBestWeekRepository>();
        var processor = CreateProcessor(
            repository: repository,
            distributedLock: CreateDistributedLock(archivingInProgress: true));

        await processor.RefreshWeeklyAsync(CancellationToken.None);

        await repository.DidNotReceive().GetCurrentWeek();
        await repository.DidNotReceive().Create(Arg.Any<Models.TheBestWeek>());
    }

    [Fact]
    public async Task RefreshWeeklyAsync_WhenLockNotAcquired_SkipsRefresh()
    {
        var repository = Substitute.For<ITheBestWeekRepository>();
        var processor = CreateProcessor(
            repository: repository,
            distributedLock: CreateDistributedLock(acquireRefreshLock: false));

        await processor.RefreshWeeklyAsync(CancellationToken.None);

        await repository.DidNotReceive().GetCurrentWeek();
        await repository.DidNotReceive().Create(Arg.Any<Models.TheBestWeek>());
    }

    [Fact]
    public async Task RefreshWeeklyAsync_WhenCityListIsEmpty_SkipsRefresh()
    {
        var repository = Substitute.For<ITheBestWeekRepository>();
        var loadingCities = Substitute.For<ILoadingCityService>();
        loadingCities.GetCityNames().Returns(new HashSet<string>());

        var processor = CreateProcessor(
            repository: repository,
            loadingCities: loadingCities);

        await processor.RefreshWeeklyAsync(CancellationToken.None);

        await repository.DidNotReceive().GetCurrentWeek();
        await repository.DidNotReceive().Create(Arg.Any<Models.TheBestWeek>());
    }

    private static TheBestWeekProcessor CreateProcessor(
        ITheBestWeekRepository? repository = null,
        ILoadingCityService? loadingCities = null,
        IRedisDistributedLock? distributedLock = null)
    {
        var cities = loadingCities ?? CreateDefaultLoadingCities();
        var lockImpl = distributedLock ?? CreateDistributedLock();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        return new TheBestWeekProcessor(
            repository ?? Substitute.For<ITheBestWeekRepository>(),
            cities,
            new TheBestWeekLockService(lockImpl),
            new ArchivingLockService(lockImpl),
            NullLogger<TheBestWeekProcessor>.Instance,
            Substitute.For<ISeasonRepository>(),
            new NormalizeCityNameService(),
            redis,
            Substitute.For<IPhotoProfileRepository>(),
            new FakeApplicationClock(new DateTime(2026, 6, 1)));
    }

    private static ILoadingCityService CreateDefaultLoadingCities()
    {
        var loadingCities = Substitute.For<ILoadingCityService>();
        loadingCities.GetCityNames().Returns(new HashSet<string> { "Moscow" });
        return loadingCities;
    }

    private static IRedisDistributedLock CreateDistributedLock(
        bool archivingInProgress = false,
        bool acquireRefreshLock = true)
    {
        var distributedLock = Substitute.For<IRedisDistributedLock>();
        distributedLock
            .IsLockedAsync(DistributedLockKeys.Archive, Arg.Any<CancellationToken>())
            .Returns(archivingInProgress);

        distributedLock
            .TryAcquireAsync(
                DistributedLockKeys.TheBestWeekRefresh,
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(acquireRefreshLock ? new TestDistributedLockHandle() : null);

        return distributedLock;
    }
}
