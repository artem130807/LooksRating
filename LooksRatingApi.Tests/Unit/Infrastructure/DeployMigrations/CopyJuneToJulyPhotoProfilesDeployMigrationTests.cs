using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.DeployMigrations;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Unit.Infrastructure.DeployMigrations;

public sealed class CopyJuneToJulyPhotoProfilesDeployMigrationTests
{
    [Fact]
    public async Task ApplyAsync_CopiesProfilesWithZeroRating_AndSkipsExistingTargetUsers()
    {
        await using var context = CreateContext();
        var (chapter, _) = await TestDataBuilder.SeedOpenSeasonAsync(context);

        var sourceSeason = Season.Create("Потный июнь", 6, chapter.Id).Value;
        sourceSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.SourceSeasonId;
        var targetSeason = Season.Create("Обгоревший июль", 7, chapter.Id).Value;
        targetSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId;
        context.Seasons.AddRange(sourceSeason, targetSeason);

        var userWithProfile = await TestDataBuilder.SeedUserAsync(context, 7001);
        var userAlreadyInTarget = await TestDataBuilder.SeedUserAsync(context, 7002);
        var archivedUser = await TestDataBuilder.SeedUserAsync(context, 7003);
        var rejectedUser = await TestDataBuilder.SeedUserAsync(context, 7004);

        var sourceProfile = CreateSourceProfile(userWithProfile, sourceSeason, rating: 8.5m, ratingCount: 12, photoCount: 2);
        var archivedProfile = CreateSourceProfile(archivedUser, sourceSeason, rating: 6m, ratingCount: 3, photoCount: 1, StatusEnum.Archived);
        var rejectedProfile = CreateSourceProfile(rejectedUser, sourceSeason, rating: 9m, ratingCount: 20, photoCount: 1, StatusEnum.Rejected);
        context.PhotoProfiles.AddRange(sourceProfile, archivedProfile, rejectedProfile);

        var existingTargetProfile = CreateSourceProfile(userAlreadyInTarget, targetSeason, rating: 5m, ratingCount: 1, photoCount: 1);
        context.PhotoProfiles.Add(existingTargetProfile);
        await context.SaveChangesAsync();

        var migration = CreateMigration(context);
        var completed = await migration.ApplyAsync();

        completed.Should().BeTrue();

        var targetProfiles = await context.PhotoProfiles
            .Include(p => p.Photos)
            .Where(p => p.SeasonId == CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId)
            .OrderBy(p => p.UserId)
            .ToListAsync();

        targetProfiles.Should().HaveCount(3);
        targetProfiles.Should().ContainSingle(p => p.UserId == userAlreadyInTarget.Id && p.Rating == 5m);

        var migrated = targetProfiles
            .Where(p => p.UserId != userAlreadyInTarget.Id)
            .ToList();

        migrated.Should().HaveCount(2);
        migrated.Should().OnlyContain(p =>
            p.Rating == 0m
            && p.RatingCount == 0
            && p.Rank == RankEnum.Terrible
            && p.Status == StatusEnum.Active
            && p.Photos.Count > 0);

        migrated.SelectMany(p => p.Photos).Should().OnlyContain(photo =>
            !string.IsNullOrWhiteSpace(photo.TelegramFileId));

        var rejectedCopied = await context.PhotoProfiles.AnyAsync(p =>
            p.SeasonId == CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId
            && p.UserId == rejectedUser.Id);
        rejectedCopied.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_IsIdempotent_WhenTargetProfilesAlreadyExist()
    {
        await using var context = CreateContext();
        var (chapter, _) = await TestDataBuilder.SeedOpenSeasonAsync(context);

        var sourceSeason = Season.Create("Потный июнь", 6, chapter.Id).Value;
        sourceSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.SourceSeasonId;
        var targetSeason = Season.Create("Обгоревший июль", 7, chapter.Id).Value;
        targetSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId;
        context.Seasons.AddRange(sourceSeason, targetSeason);

        var user = await TestDataBuilder.SeedUserAsync(context, 7101);
        var sourceProfile = CreateSourceProfile(user, sourceSeason, rating: 7m, ratingCount: 4, photoCount: 2);
        var targetProfile = CreateSourceProfile(user, targetSeason, rating: 0m, ratingCount: 0, photoCount: 1);
        targetProfile.Rank = RankEnum.Terrible;
        context.PhotoProfiles.AddRange(sourceProfile, targetProfile);
        await context.SaveChangesAsync();

        var migration = CreateMigration(context);
        var completed = await migration.ApplyAsync();

        completed.Should().BeTrue();

        var count = await context.PhotoProfiles.CountAsync(p =>
            p.SeasonId == CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId
            && p.UserId == user.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task ApplyAsync_ContinuesWhenRedisPopulationFails()
    {
        await using var context = CreateContext();
        var (chapter, _) = await TestDataBuilder.SeedOpenSeasonAsync(context);

        var sourceSeason = Season.Create("Потный июнь", 6, chapter.Id).Value;
        sourceSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.SourceSeasonId;
        var targetSeason = Season.Create("Обгоревший июль", 7, chapter.Id).Value;
        targetSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId;
        context.Seasons.AddRange(sourceSeason, targetSeason);

        var user = await TestDataBuilder.SeedUserAsync(context, 7201);
        context.PhotoProfiles.Add(CreateSourceProfile(user, sourceSeason, rating: 8m, ratingCount: 5, photoCount: 1));
        await context.SaveChangesAsync();

        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        database
            .HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>())
            .Returns<Task>(_ => throw new RedisException("redis unavailable"));

        var normalizeCityNameService = Substitute.For<INormalizeCityNameService>();
        normalizeCityNameService.Normalize(Arg.Any<string>()).Returns(call => call.Arg<string>());

        var migration = new CopyJuneToJulyPhotoProfilesDeployMigration(
            context,
            redis,
            normalizeCityNameService,
            NullLogger<CopyJuneToJulyPhotoProfilesDeployMigration>.Instance);

        var completed = await migration.ApplyAsync();
        completed.Should().BeTrue();

        var copied = await context.PhotoProfiles.SingleAsync(p =>
            p.SeasonId == CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId
            && p.UserId == user.Id);
        copied.Rating.Should().Be(0m);
        copied.RatingCount.Should().Be(0);
    }

    private static CopyJuneToJulyPhotoProfilesDeployMigration CreateMigration(LooksRatingDbContext context)
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        var normalizeCityNameService = Substitute.For<INormalizeCityNameService>();
        normalizeCityNameService.Normalize(Arg.Any<string>()).Returns(call => call.Arg<string>());

        return new CopyJuneToJulyPhotoProfilesDeployMigration(
            context,
            redis,
            normalizeCityNameService,
            NullLogger<CopyJuneToJulyPhotoProfilesDeployMigration>.Instance);
    }

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new LooksRatingDbContext(options);
    }

    private static PhotoProfile CreateSourceProfile(
        User user,
        Season season,
        decimal rating,
        int ratingCount,
        int photoCount,
        StatusEnum status = StatusEnum.Active)
    {
        var city = CityVo.Create("moscow").Value;
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SeasonId = season.Id,
            Rating = rating,
            RatingCount = ratingCount,
            Rank = RankEnum.Cute,
            Status = status,
            CityNomination = city,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Male,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
        };

        for (var i = 0; i < photoCount; i++)
        {
            profile.Photos.Add(new PhotoProfilePhoto
            {
                Id = Guid.NewGuid(),
                PhotoProfileId = profile.Id,
                TelegramFileId = $"file-{profile.Id:N}-{i}",
                SortOrder = i,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
            });
        }

        return profile;
    }
}
