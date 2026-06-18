using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

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
        var position = await repository.GetSeasonTopPositionAsync(target, seasonIsClosed: false);

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
            var position = await repository.GetSeasonTopPositionAsync(profile, seasonIsClosed: false);

            position.Should().NotBeNull();
            position!.Place.Should().Be(index + 1);
            position.TotalCount.Should().Be(3);
        }
    }

    private static PhotoProfile CreateRankedProfile(
        User user,
        Season season,
        CityVo city,
        decimal rating,
        int ratingCount)
    {
        return new PhotoProfile
        {
            Id = Guid.NewGuid(),
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
            CreatedAt = DateTime.UtcNow,
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
