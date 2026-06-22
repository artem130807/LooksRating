using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Integration.Repositories;

[Collection(IntegrationCollection.Name)]
public sealed class PhotoProfileRepositoryTests
{
    private readonly PostgresFixture _postgres;

    public PhotoProfileRepositoryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [SkippableFact]
    public async Task GetParticipantCountsBySeasonIdsAsync_CountsProfilesNotPhotos()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user1 = await TestDataBuilder.SeedUserAsync(context, 1001);
        var user2 = await TestDataBuilder.SeedUserAsync(context, 1002);
        var user3 = await TestDataBuilder.SeedUserAsync(context, 1003);

        await TestDataBuilder.SeedPhotoProfileAsync(context, user1, season, photoCount: 3);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user2, season, photoCount: 2);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user3, season, photoCount: 1);

        var repository = new PhotoProfileRepository(context);
        var counts = await repository.GetParticipantCountsBySeasonIdsAsync(new[] { season.Id });

        counts[season.Id].Should().Be(3);
    }

    [SkippableFact]
    public async Task GetParticipantCountsBySeasonIdsAsync_ExcludesRejected_IncludesArchived()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var activeUser = await TestDataBuilder.SeedUserAsync(context, 2001);
        var archivedUser = await TestDataBuilder.SeedUserAsync(context, 2002);
        var rejectedUser = await TestDataBuilder.SeedUserAsync(context, 2003);

        await TestDataBuilder.SeedPhotoProfileAsync(context, activeUser, season, StatusEnum.Active);
        await TestDataBuilder.SeedPhotoProfileAsync(context, archivedUser, season, StatusEnum.Archived);
        await TestDataBuilder.SeedPhotoProfileAsync(context, rejectedUser, season, StatusEnum.Rejected);

        var repository = new PhotoProfileRepository(context);
        var counts = await repository.GetParticipantCountsBySeasonIdsAsync(new[] { season.Id });

        counts[season.Id].Should().Be(2);
    }

    [SkippableFact]
    public async Task CountSeasonsWithProfileAsync_CountsDistinctSeasonsForUser()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season1) = await TestDataBuilder.SeedOpenSeasonAsync(context, seasonNumber: 1);
        var chapter2 = ListSeasons.Create().Value;
        var season2 = Season.Create("Season 2", 2, chapter2.Id).Value;
        context.ListSeasons.Add(chapter2);
        context.Seasons.Add(season2);
        await context.SaveChangesAsync();

        var user = await TestDataBuilder.SeedUserAsync(context, 3001);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user, season1);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user, season2);

        var repository = new PhotoProfileRepository(context);
        var count = await repository.CountSeasonsWithProfileAsync(user.Id);

        count.Should().Be(2);
    }

    [SkippableFact]
    public async Task CountSeasonsWithProfileAsync_ExcludesRejectedProfiles()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season1) = await TestDataBuilder.SeedOpenSeasonAsync(context, seasonNumber: 1);
        var chapter2 = ListSeasons.Create().Value;
        var season2 = Season.Create("Season 2", 2, chapter2.Id).Value;
        context.ListSeasons.Add(chapter2);
        context.Seasons.Add(season2);
        await context.SaveChangesAsync();

        var user = await TestDataBuilder.SeedUserAsync(context, 3002);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user, season1);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user, season2, StatusEnum.Rejected);

        var repository = new PhotoProfileRepository(context);
        var count = await repository.CountSeasonsWithProfileAsync(user.Id);

        count.Should().Be(1);
    }

    [SkippableFact]
    public async Task ArchiveProfilesAsync_UpdatesStatusInDatabase()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 4001);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season, StatusEnum.Active);

        var repository = new PhotoProfileRepository(context);
        await repository.ArchiveProfilesAsync(new List<Guid> { profile.Id });

        var status = await context.PhotoProfiles
            .Where(p => p.Id == profile.Id)
            .Select(p => p.Status)
            .SingleAsync();

        status.Should().Be(StatusEnum.Archived);
    }

    [SkippableFact]
    public async Task GetSeasonTopPositionAsync_ReturnsPlaceWithinNominationCategory()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var leaderUser = await TestDataBuilder.SeedUserAsync(context, 5101);
        var targetUser = await TestDataBuilder.SeedUserAsync(context, 5102);
        var outsiderUser = await TestDataBuilder.SeedUserAsync(context, 5103);

        var city = CityVo.Create("ulyanovsk").Value;
        var leader = CreateRankedProfile(leaderUser, season, city, rating: 10m, ratingCount: 20);
        var target = CreateRankedProfile(targetUser, season, city, rating: 9.5m, ratingCount: 15);
        var outsider = CreateRankedProfile(
            outsiderUser,
            season,
            CityVo.Create("moscow").Value,
            rating: 10m,
            ratingCount: 30);

        context.PhotoProfiles.AddRange(leader, target, outsider);
        await context.SaveChangesAsync();

        var repository = new PhotoProfileRepository(context);
        var topReadService = CreateTopReadService(context);
        var position = await topReadService.GetSeasonTopPositionAsync(target, seasonIsClosed: false);

        position.Should().NotBeNull();
        position!.Place.Should().Be(2);
        position.TotalCount.Should().Be(2);
    }

    [SkippableFact]
    public async Task GetSeasonTopPositionAsync_MatchesTopOrdering()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var users = new[]
        {
            await TestDataBuilder.SeedUserAsync(context, 5201),
            await TestDataBuilder.SeedUserAsync(context, 5202),
            await TestDataBuilder.SeedUserAsync(context, 5203),
        };

        var city = CityVo.Create("kazan").Value;
        var profiles = new[]
        {
            CreateRankedProfile(users[0], season, city, rating: 10m, ratingCount: 30),
            CreateRankedProfile(users[1], season, city, rating: 9.8m, ratingCount: 20),
            CreateRankedProfile(users[2], season, city, rating: 9.5m, ratingCount: 15),
        };

        context.PhotoProfiles.AddRange(profiles);
        await context.SaveChangesAsync();

        var repository = new PhotoProfileRepository(context);
        var topReadService = CreateTopReadService(context);
        var rankedIds = await repository.GetTopProfileIdsAsync(
            season.Id,
            seasonIsClosed: false,
            city.Value!,
            GenderEnum.Male,
            age: 18,
            skip: 0,
            take: 10);

        for (var index = 0; index < rankedIds.Count; index++)
        {
            var profile = profiles.Single(p => p.Id == rankedIds[index]);
            var position = await topReadService.GetSeasonTopPositionAsync(profile, seasonIsClosed: false);

            position.Should().NotBeNull();
            position!.Place.Should().Be(index + 1);
            position.TotalCount.Should().Be(3);
        }
    }

    [SkippableFact]
    public async Task GetSeasonTopPositionAsync_AssignsDistinctPlacesWhenRatingsAreEqual()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var users = new[]
        {
            await TestDataBuilder.SeedUserAsync(context, 5301),
            await TestDataBuilder.SeedUserAsync(context, 5302),
            await TestDataBuilder.SeedUserAsync(context, 5303),
        };

        var city = CityVo.Create("samara").Value;
        var oldestCreatedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var middleCreatedAt = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc);
        var newestCreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var profiles = new[]
        {
            CreateRankedProfile(users[0], season, city, rating: 8m, ratingCount: 0, oldestCreatedAt),
            CreateRankedProfile(users[1], season, city, rating: 8m, ratingCount: 0, middleCreatedAt),
            CreateRankedProfile(users[2], season, city, rating: 8m, ratingCount: 0, newestCreatedAt),
        };

        context.PhotoProfiles.AddRange(profiles);
        await context.SaveChangesAsync();

        var repository = new PhotoProfileRepository(context);
        var topReadService = CreateTopReadService(context);
        var rankedIds = await repository.GetTopProfileIdsAsync(
            season.Id,
            seasonIsClosed: false,
            city.Value!,
            GenderEnum.Male,
            age: 18,
            skip: 0,
            take: 10);

        rankedIds.Should().HaveCount(3);
        rankedIds[0].Should().Be(profiles[2].Id);
        rankedIds[1].Should().Be(profiles[1].Id);
        rankedIds[2].Should().Be(profiles[0].Id);

        foreach (var profile in profiles)
        {
            var position = await topReadService.GetSeasonTopPositionAsync(profile, seasonIsClosed: false);
            position.Should().NotBeNull();
            position!.Place.Should().Be(rankedIds.IndexOf(profile.Id) + 1);
            position.TotalCount.Should().Be(3);
        }
    }

    [SkippableFact]
    public async Task CountFeedProfilesAsync_ExcludesProfileIds()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 6051);
        var city = CityVo.Create("moscow").Value;
        var profiles = new List<PhotoProfile>();
        for (var i = 0; i < 5; i++)
        {
            var user = await TestDataBuilder.SeedUserAsync(context, 6150 + i);
            profiles.Add(CreateFeedProfile(user, season, city, age: 25));
        }

        context.PhotoProfiles.AddRange(profiles);
        await context.SaveChangesAsync();

        var repository = new PhotoProfileRepository(context);
        var totalCount = await repository.CountFeedProfilesAsync(
            season.Id,
            reviewer.Id,
            city.Value!,
            GenderEnum.Male,
            age: 25);
        var availableCount = await repository.CountFeedProfilesAsync(
            season.Id,
            reviewer.Id,
            city.Value!,
            GenderEnum.Male,
            age: 25,
            excludeProfileIds: new[] { profiles[0].Id, profiles[1].Id });

        totalCount.Should().Be(5);
        availableCount.Should().Be(3);
    }

    [SkippableFact]
    public async Task GetRandomFeedCandidateProfileIdsAsync_ExcludesProfileIds()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 6001);
        var city = CityVo.Create("moscow").Value;
        var profiles = new List<PhotoProfile>();
        for (var i = 0; i < 5; i++)
        {
            var user = await TestDataBuilder.SeedUserAsync(context, 6100 + i);
            profiles.Add(CreateFeedProfile(user, season, city, age: 25));
        }

        context.PhotoProfiles.AddRange(profiles);
        await context.SaveChangesAsync();

        var excluded = new[] { profiles[0].Id, profiles[1].Id };
        var repository = new PhotoProfileRepository(context);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidates = await repository.GetRandomFeedCandidateProfileIdsAsync(
                season.Id,
                reviewer.Id,
                city.Value!,
                GenderEnum.Male,
                age: 25,
                take: 10,
                excludeProfileIds: excluded);

            candidates.Should().NotBeEmpty();
            candidates.Should().NotContain(excluded);
        }
    }

    [SkippableFact]
    public async Task GetRandomNewFeedCandidateProfileIdsAsync_ExcludesProfileIds()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 6301);
        var city = CityVo.Create("moscow").Value;
        var anchor = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var profiles = new List<PhotoProfile>();
        for (var i = 0; i < 5; i++)
        {
            var user = await TestDataBuilder.SeedUserAsync(context, 6310 + i);
            profiles.Add(CreateFeedProfile(
                user,
                season,
                city,
                age: 25,
                createdAt: anchor.AddHours(i + 1)));
        }

        context.PhotoProfiles.AddRange(profiles);
        await context.SaveChangesAsync();

        var excluded = new[] { profiles[2].Id, profiles[3].Id };
        var repository = new PhotoProfileRepository(context);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidates = await repository.GetRandomNewFeedCandidateProfileIdsAsync(
                season.Id,
                reviewer.Id,
                city.Value!,
                GenderEnum.Male,
                age: 25,
                createdAfter: anchor,
                take: 10,
                excludeProfileIds: excluded);

            candidates.Should().NotBeEmpty();
            candidates.Should().NotContain(excluded);
        }
    }

    private static PhotoProfile CreateFeedProfile(
        User user,
        Season season,
        CityVo city,
        int age,
        DateTime? createdAt = null)
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
            CreatedAt = createdAt ?? DateTime.UtcNow,
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

    private static PhotoTopReadService CreateTopReadService(LooksRatingDbContext context)
    {
        var database = Substitute.For<IDatabase>();
        database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);

        return new PhotoTopReadService(
            database,
            new NormalizeCityNameService(),
            new PhotoProfileRepository(context));
    }

    private static PhotoProfile CreateRankedProfile(
        User user,
        Season season,
        CityVo city,
        decimal rating,
        int ratingCount,
        DateTime? createdAt = null,
        Guid? id = null)
    {
        return new PhotoProfile
        {
            Id = id ?? Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            SeasonId = season.Id,
            Rating = rating,
            RatingCount = ratingCount,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = city,
            AgeNomination = 18,
            GenderNomination = GenderEnum.Male,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Photos =
            {
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    TelegramFileId = $"file-{user.TelegramId}",
                    SortOrder = 0,
                },
            },
        };
    }
}

