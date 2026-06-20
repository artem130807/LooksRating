using CSharpFunctionalExtensions;
using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Integration.Services.PhotoServices;

[Collection(IntegrationCollection.Name)]
public sealed class PhotoRecommendationServiceTests
{
    private readonly PostgresFixture _postgres;
    private readonly RedisFixture _redis;

    public PhotoRecommendationServiceTests(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
    }

    [SkippableFact]
    public async Task DoesNotReturnRatedProfileInCurrentCycle()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 7001);
        var profiles = await SeedFeedProfilesAsync(context, season, count: 3);
        var ratedProfileId = profiles[0].Id;

        await SeedRatedSetAsync(reviewer.Id, season.Id, ratedProfileId);
        var service = CreateService(context, season);

        for (var attempt = 0; attempt < 15; attempt++)
        {
            var result = await service.GetNextUnratedProfileIdsAsync(
                reviewer.Id,
                GenderEnum.Male,
                age: 25,
                city: "moscow");

            result.Should().ContainSingle();
            result[0].Should().NotBe(ratedProfileId);
        }
    }

    [SkippableFact]
    public async Task ReturnsRemainingCandidateWithoutResettingRatedSet()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 7101);
        var profiles = await SeedFeedProfilesAsync(context, season, count: 3);
        var remainingProfileId = profiles[2].Id;

        await SeedRatedSetAsync(reviewer.Id, season.Id, profiles[0].Id, profiles[1].Id);
        var service = CreateService(context, season);

        var result = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");

        result.Should().ContainSingle();
        result[0].Should().Be(remainingProfileId);

        var ratedAfter = await GetRatedSetAsync(reviewer.Id, season.Id);
        ratedAfter.Should().BeEquivalentTo(new[] { profiles[0].Id, profiles[1].Id });
    }

    [SkippableFact]
    public async Task RestartsCycleWhenRemainingCandidatesAreSkipped()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 7201);
        var profiles = await SeedFeedProfilesAsync(context, season, count: 3);

        await SeedRatedSetAsync(reviewer.Id, season.Id, profiles[0].Id);

        var service = CreateService(context, season);

        var result = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow",
            skipProfileIds: new[] { profiles[1].Id, profiles[2].Id });

        result.Should().ContainSingle();
        result[0].Should().Be(profiles[0].Id);

        var ratedAfter = await GetRatedSetAsync(reviewer.Id, season.Id);
        ratedAfter.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task RestartsCycleWhenAllAvailableProfilesRated_UnviewableExcludedFromFeedCount()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 7251);
        var profiles = await SeedFeedProfilesAsync(context, season, count: 3);

        await SeedRatedSetAsync(reviewer.Id, season.Id, profiles[0].Id);

        var unviewable = Substitute.For<IUnviewablePhotosProfilesService>();
        unviewable.GetUnviewablePhotosProfile(reviewer.Id)
            .Returns(Result.Success(new List<Guid> { profiles[1].Id, profiles[2].Id }));

        var service = CreateService(context, season, unviewable);

        var result = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");

        result.Should().ContainSingle();
        result[0].Should().Be(profiles[0].Id);

        var ratedAfter = await GetRatedSetAsync(reviewer.Id, season.Id);
        ratedAfter.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task RestartsCycleOnlyWhenAllNominationProfilesRated()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 7301);
        var profiles = await SeedFeedProfilesAsync(context, season, count: 3);

        await SeedRatedSetAsync(
            reviewer.Id,
            season.Id,
            profiles[0].Id,
            profiles[1].Id,
            profiles[2].Id);

        var service = CreateService(context, season);

        var result = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");

        result.Should().ContainSingle();
        profiles.Select(p => p.Id).Should().Contain(result[0]);

        var ratedAfter = await GetRatedSetAsync(reviewer.Id, season.Id);
        ratedAfter.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task VipFeedTurn_RequestsVipOnlyCandidates()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 7401);
        var city = CityVo.Create("moscow").Value;
        var regularUsers = new[]
        {
            await TestDataBuilder.SeedUserAsync(context, 7410),
            await TestDataBuilder.SeedUserAsync(context, 7411),
        };
        var vipUser = await TestDataBuilder.SeedUserAsync(context, 7412, VipStatus.Availlable);

        var regularProfiles = regularUsers
            .Select(user => CreateFeedProfile(user, season, city, age: 25))
            .ToList();
        var vipProfile = CreateFeedProfile(vipUser, season, city, age: 25);

        context.PhotoProfiles.AddRange(regularProfiles);
        context.PhotoProfiles.Add(vipProfile);
        await context.SaveChangesAsync();

        await SetFeedRatingCounterAsync(reviewer.Id, season.Id, 4);

        var service = CreateService(context, season);
        var result = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");

        result.Should().ContainSingle();
        result[0].Should().Be(vipProfile.Id);
    }

    [SkippableFact]
    public async Task RepairsRatedSetFromReviews_WhenRedisSetEmpty()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 7501);
        var profiles = await SeedFeedProfilesAsync(context, season, count: 3);
        var reviewedProfileId = profiles[0].Id;

        var review = Review.Create(8, reviewer.Id, reviewedProfileId).Value;
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        await ClearRatedSetAsync(reviewer.Id, season.Id);

        var service = CreateService(context, season);
        var result = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");

        result.Should().ContainSingle();
        result[0].Should().NotBe(reviewedProfileId);

        var ratedAfter = await GetRatedSetAsync(reviewer.Id, season.Id);
        ratedAfter.Should().ContainSingle().Which.Should().Be(reviewedProfileId);
    }

    private PhotoRecommendationService CreateService(
        LooksRatingDbContext context,
        Season season,
        IUnviewablePhotosProfilesService? unviewableService = null)
    {
        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var cityService = Substitute.For<ICityService>();
        cityService
            .TryResolveCanonicalCity(Arg.Any<string>(), out Arg.Any<string?>())
            .Returns(callInfo =>
            {
                callInfo[1] = "moscow";
                return true;
            });

        var unviewable = unviewableService ?? Substitute.For<IUnviewablePhotosProfilesService>();
        if (unviewableService is null)
        {
            unviewable.GetUnviewablePhotosProfile(Arg.Any<Guid>())
                .Returns(Result.Success(new List<Guid>()));
        }

        return new PhotoRecommendationService(
            new FeedCycleRedisStore(_redis.Connection),
            new NormalizeCityNameService(),
            cityService,
            seasonRepository,
            new PhotoProfileRepository(context),
            new ReviewRepository(context),
            unviewable,
            NullLogger<PhotoRecommendationService>.Instance);
    }

    private static async Task<List<PhotoProfile>> SeedFeedProfilesAsync(
        LooksRatingDbContext context,
        Season season,
        int count)
    {
        var city = CityVo.Create("moscow").Value;
        var profiles = new List<PhotoProfile>();
        for (var i = 0; i < count; i++)
        {
            var user = await TestDataBuilder.SeedUserAsync(context, 7600 + i);
            profiles.Add(CreateFeedProfile(user, season, city, age: 25));
        }

        context.PhotoProfiles.AddRange(profiles);
        await context.SaveChangesAsync();
        return profiles;
    }

    private static PhotoProfile CreateFeedProfile(
        User user,
        Season season,
        CityVo city,
        int age)
    {
        return new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            SeasonId = season.Id,
            Rating = 7m,
            RatingCount = 5,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = city,
            AgeNomination = age,
            GenderNomination = GenderEnum.Male,
            CreatedAt = DateTime.UtcNow,
            Photos =
            {
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    TelegramFileId = $"feed-file-{user.TelegramId}",
                    SortOrder = 0,
                },
            },
        };
    }

    private async Task SeedRatedSetAsync(Guid reviewerId, Guid seasonId, params Guid[] profileIds)
    {
        var db = _redis.Connection.GetDatabase();
        var key = PhotoRedisKeys.UserRatedSet(reviewerId, seasonId);
        await db.KeyDeleteAsync(key);
        if (profileIds.Length > 0)
        {
            var values = profileIds.Select(id => (RedisValue)id.ToString()).ToArray();
            await db.SetAddAsync(key, values);
        }
    }

    private async Task ClearRatedSetAsync(Guid reviewerId, Guid seasonId)
    {
        await _redis.Connection.GetDatabase()
            .KeyDeleteAsync(PhotoRedisKeys.UserRatedSet(reviewerId, seasonId));
    }

    private async Task SetFeedRatingCounterAsync(Guid reviewerId, Guid seasonId, int value)
    {
        await _redis.Connection.GetDatabase()
            .StringSetAsync(PhotoRedisKeys.FeedRatingCounter(reviewerId, seasonId), value.ToString());
    }

    private async Task<HashSet<Guid>> GetRatedSetAsync(Guid reviewerId, Guid seasonId)
    {
        var members = await _redis.Connection.GetDatabase()
            .SetMembersAsync(PhotoRedisKeys.UserRatedSet(reviewerId, seasonId));

        var rated = new HashSet<Guid>();
        foreach (var member in members)
        {
            if (Guid.TryParse(member.ToString(), out var profileId))
            {
                rated.Add(profileId);
            }
        }

        return rated;
    }
}
