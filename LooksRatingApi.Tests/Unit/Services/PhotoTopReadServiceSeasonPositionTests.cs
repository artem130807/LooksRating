using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class PhotoTopReadServiceSeasonPositionTests
{
    [Fact]
    public async Task GetSeasonTopPositionAsync_ReturnsIndexFromTopList()
    {
        var seasonId = Guid.NewGuid();
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var thirdId = Guid.Parse("00000000-0000-0000-0000-000000000003");

        var repository = Substitute.For<IPhotoProfileRepository>();
        repository
            .GetTopProfileIdsAsync(
                seasonId,
                false,
                "kazan",
                GenderEnum.Male,
                17,
                0,
                int.MaxValue,
                false,
                Arg.Any<CancellationToken>())
            .Returns([firstId, secondId, thirdId]);
        repository
            .CountTopProfilesAsync(
                seasonId,
                false,
                "kazan",
                GenderEnum.Male,
                17,
                false,
                Arg.Any<CancellationToken>())
            .Returns(3);

        var service = CreateService(repository);
        var profile = CreateProfile(secondId, seasonId, "kazan");

        var position = await service.GetSeasonTopPositionAsync(profile, seasonIsClosed: false);

        position.Should().NotBeNull();
        position!.Place.Should().Be(2);
        position.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetSeasonTopPositionAsync_AssignsDistinctPlacesWhenRatingsAreEqual()
    {
        var seasonId = Guid.NewGuid();
        var oldestId = Guid.NewGuid();
        var middleId = Guid.NewGuid();
        var newestId = Guid.NewGuid();
        var oldestCreatedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var middleCreatedAt = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc);
        var newestCreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        var repository = Substitute.For<IPhotoProfileRepository>();
        repository
            .GetTopProfileIdsAsync(
                seasonId,
                false,
                "samara",
                GenderEnum.Male,
                17,
                0,
                int.MaxValue,
                false,
                Arg.Any<CancellationToken>())
            .Returns([newestId, middleId, oldestId]);
        repository
            .CountTopProfilesAsync(
                seasonId,
                false,
                "samara",
                GenderEnum.Male,
                17,
                false,
                Arg.Any<CancellationToken>())
            .Returns(3);

        var service = CreateService(repository);

        var oldest = CreateProfile(oldestId, seasonId, "samara", oldestCreatedAt);
        var middle = CreateProfile(middleId, seasonId, "samara", middleCreatedAt);
        var newest = CreateProfile(newestId, seasonId, "samara", newestCreatedAt);

        (await service.GetSeasonTopPositionAsync(oldest, false))!.Place.Should().Be(3);
        (await service.GetSeasonTopPositionAsync(middle, false))!.Place.Should().Be(2);
        (await service.GetSeasonTopPositionAsync(newest, false))!.Place.Should().Be(1);
    }

    private static PhotoTopReadService CreateService(IPhotoProfileRepository repository)
    {
        var database = Substitute.For<IDatabase>();
        database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);

        return new PhotoTopReadService(
            database,
            new NormalizeCityNameService(),
            repository);
    }

    private static PhotoProfile CreateProfile(Guid id, Guid seasonId, string city, DateTime? createdAt = null)
    {
        return new PhotoProfile
        {
            Id = id,
            UserId = Guid.NewGuid(),
            SeasonId = seasonId,
            Rating = 8m,
            RatingCount = 0,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create(city).Value,
            AgeNomination = 18,
            GenderNomination = GenderEnum.Male,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };
    }
}
