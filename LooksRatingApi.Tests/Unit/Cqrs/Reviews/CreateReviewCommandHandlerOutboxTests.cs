using CSharpFunctionalExtensions;
using Hangfire;
using LooksRatingApi;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Cqrs.Reviews;

public sealed class CreateReviewCommandHandlerOutboxTests
{
    [Fact]
    public async Task Handle_WhenReviewSaved_CreatesOutboxRecordAndEnqueuesHangfireJob()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new LooksRatingDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 9001,
            TelegramUsername = "reviewer",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable
        };
        var owner = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 9002,
            TelegramUsername = "owner",
            Name = "Owner",
            Status = VipStatus.Unavaillable
        };
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = owner.Id,
            User = owner,
            SeasonId = Guid.NewGuid(),
            Rating = 0m,
            RatingCount = 0,
            Rank = RankEnum.Terrible,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(reviewer, owner);
        context.PhotoProfiles.Add(profile);
        await context.SaveChangesAsync();

        var validator = Substitute.For<ICreateReviewValidator>();
        validator.ValidateAsync(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var userRepository = new UserRepository(context);
        var photoProfileRepository = new PhotoProfileRepository(context);
        var reviewRepository = new ReviewRepository(context);
        var backgroundJobClient = Substitute.For<IBackgroundJobClient>();

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            new RankService(),
            backgroundJobClient,
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(reviewer.TelegramId, profile.Id, 8),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var outbox = await context.OutboxMessages.SingleAsync();
        outbox.MessageType.Should().Be(CreateReviewOutboxMessage.Type);
        outbox.Status.Should().Be(OutboxMessageStatus.Pending);
        outbox.TryReadPayload<CreateReviewOutboxPayload>(out var payload).Should().BeTrue();
        payload!.ReviewId.Should().Be(result.Value.ReviewId);
        payload.PhotoProfileId.Should().Be(profile.Id);
        payload.ReviewerUserId.Should().Be(reviewer.Id);

        backgroundJobClient.Received(1).Enqueue<IReviewBackgroundService>(
            Arg.Any<System.Linq.Expressions.Expression<Action<IReviewBackgroundService>>>());

        await connection.DisposeAsync();
    }
}
