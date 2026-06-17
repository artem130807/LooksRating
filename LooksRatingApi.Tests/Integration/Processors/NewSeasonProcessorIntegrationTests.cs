using LooksRatingApi.Contracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fakes;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Integration.Processors;

[Collection(IntegrationCollection.Name)]
public sealed class NewSeasonProcessorIntegrationTests
{
    private readonly PostgresFixture _postgres;
    private readonly RedisFixture _redis;

    public NewSeasonProcessorIntegrationTests(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
    }

    [SkippableFact]
    public async Task ProcessMonthlyRolloverAsync_OnFirstDay_ArchivesProfilesAndOpensNextSeason()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, januarySeason) = await TestDataBuilder.SeedOpenSeasonAsync(context, seasonNumber: 1, seasonName: "January");
        var user1 = await TestDataBuilder.SeedUserAsync(context, 6001);
        var user2 = await TestDataBuilder.SeedUserAsync(context, 6002);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user1, januarySeason);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user2, januarySeason);

        var processor = CreateProcessor(context);
        await processor.ProcessMonthlyRolloverAsync(CancellationToken.None);

        var seasons = await context.Seasons
            .OrderBy(season => season.Number)
            .ToListAsync();

        seasons.Should().HaveCount(2);
        seasons[0].IsClosed.Should().BeTrue();
        seasons[1].Number.Should().Be(2);
        seasons[1].IsClosed.Should().BeFalse();

        var archivedCount = await context.PhotoProfiles
            .CountAsync(profile => profile.SeasonId == januarySeason.Id && profile.Status == StatusEnum.Archived);

        archivedCount.Should().Be(2);
    }

    private NewSeasonProcessor CreateProcessor(LooksRatingApi.LooksRatingDbContext context)
    {
        var loadingCities = Substitute.For<ILoadingCityService>();
        loadingCities.GetCityNames().Returns(new HashSet<string> { "Moscow" });

        return new NewSeasonProcessor(
            new PhotoProfileRepository(context),
            new SeasonRepository(context),
            new ListSeasonsRepository(context),
            loadingCities,
            new NormalizeCityNameService(),
            new ArchivingLockService(new RedisDistributedLock(_redis.Connection)),
            new SeasonTopSparksRewardProcessor(
                Substitute.For<ISeasonTopCategoryService>(),
                Substitute.For<ISparksRewardCreditingService>(),
                NullLogger<SeasonTopSparksRewardProcessor>.Instance),
            new FakeApplicationClock(new DateTime(2026, 2, 1)),
            _redis.Connection,
            NullLogger<NewSeasonProcessor>.Instance);
    }
}
