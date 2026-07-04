using Hangfire;
using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.DeployMigrations;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.Cqrs.Reviews;

public sealed class CreateReviewAfterMigrationIntegrationTests
{
    [Fact]
    public async Task Handle_CreatesReviewForMigratedZeroRatingProfile()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new LooksRatingDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var (chapter, _) = await TestDataBuilder.SeedOpenSeasonAsync(context);

        var sourceSeason = Season.Create("Потный июнь", 6, chapter.Id).Value;
        sourceSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.SourceSeasonId;
        var targetSeason = Season.Create("Обгоревший июль", 7, chapter.Id).Value;
        targetSeason.Id = CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId;
        context.Seasons.AddRange(sourceSeason, targetSeason);

        var owner = await TestDataBuilder.SeedUserAsync(context, 8101);
        var reviewer = await TestDataBuilder.SeedUserAsync(context, 8102);

        var sourceProfile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = owner.Id,
            User = owner,
            SeasonId = sourceSeason.Id,
            Rating = 8.5m,
            RatingCount = 12,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Photos =
            {
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    TelegramFileId = "file-june-1",
                    SortOrder = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                },
            },
        };
        context.PhotoProfiles.Add(sourceProfile);
        await context.SaveChangesAsync();

        var migration = new CopyJuneToJulyPhotoProfilesDeployMigration(
            context,
            Substitute.For<StackExchange.Redis.IConnectionMultiplexer>(),
            new NormalizeCityNameService(),
            NullLogger<CopyJuneToJulyPhotoProfilesDeployMigration>.Instance);
        (await migration.ApplyAsync()).Should().BeTrue();

        var migratedProfile = await context.PhotoProfiles
            .Include(p => p.User)
            .SingleAsync(p =>
                p.SeasonId == CopyJuneToJulyPhotoProfilesDeployMigration.TargetSeasonId
                && p.UserId == owner.Id);

        migratedProfile.Rating.Should().Be(0m);
        migratedProfile.RatingCount.Should().Be(0);

        var userRepository = new UserRepository(context);
        var photoProfileRepository = new PhotoProfileRepository(context);
        var reviewRepository = new ReviewRepository(context);
        var validator = new CreateReviewValidator(userRepository, photoProfileRepository, reviewRepository);

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            new RankService(),
            Substitute.For<IBackgroundJobClient>(),
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(reviewer.TelegramId, migratedProfile.Id, 9),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpdatedProfileRatingCount.Should().Be(1);
        result.Value.UpdatedProfileRating.Should().Be(9m);

        var storedReview = await context.Reviews.SingleAsync(r =>
            r.UserId == reviewer.Id && r.PhotoProfileId == migratedProfile.Id);
        storedReview.Rating.Should().Be(9);
    }
}
