using CSharpFunctionalExtensions;
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
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Cqrs.Reviews;

public sealed class CreateReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenReviewCreated_PublishesCreateReviewEvent()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new LooksRatingDbContext(options);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1001,
            TelegramUsername = "reviewer",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable,
        };
        var ownerId = Guid.NewGuid();
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            User = new User
            {
                Id = ownerId,
                TelegramId = 2002,
                TelegramUsername = "owner",
                Name = "Owner",
                Status = VipStatus.Unavaillable,
            },
            SeasonId = Guid.NewGuid(),
            Rating = 7m,
            RatingCount = 1,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };

        var validator = Substitute.For<ICreateReviewValidator>();
        validator.ValidateAsync(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(1001).Returns(reviewer);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        photoProfileRepository
            .UpdateAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository
            .GetByUserAndProfileAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns((Review?)null);
        reviewRepository
            .Create(Arg.Any<Review>())
            .Returns(Task.CompletedTask);

        var photoRatedProducer = Substitute.For<IKafkaPhotoRatedProducer<PhotoRatedEvent>>();
        var createReviewPublisher = Substitute.For<ICreateReviewEventPublisher>();
        createReviewPublisher
            .PublishAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new CreateReviewEvent(reviewer.Id, profile.Id, 1)));

        var photoRatingCache = Substitute.For<IPhotoRatingCacheService>();
        var reviewSparksReward = Substitute.For<IReviewSparksRewardService>();
        var ratedProfileSparksReward = Substitute.For<IRatedProfileSparksRewardService>();
        var addLastActiveUser = Substitute.For<IAddLastActiveUser>();

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            photoRatedProducer,
            createReviewPublisher,
            new RankService(),
            photoRatingCache,
            reviewSparksReward,
            ratedProfileSparksReward,
            addLastActiveUser,
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(1001, profile.Id, 8),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await createReviewPublisher.Received(1).PublishAsync(
            reviewer.Id,
            profile.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReviewUpdated_DoesNotPublishCreateReviewEvent()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new LooksRatingDbContext(options);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1001,
            TelegramUsername = "reviewer",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable,
        };
        var ownerId = Guid.NewGuid();
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            User = new User
            {
                Id = ownerId,
                TelegramId = 2002,
                TelegramUsername = "owner",
                Name = "Owner",
                Status = VipStatus.Unavaillable,
            },
            SeasonId = Guid.NewGuid(),
            Rating = 7m,
            RatingCount = 1,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };
        var existingReview = Review.Create(5, reviewer.Id, profile.Id).Value;

        var validator = Substitute.For<ICreateReviewValidator>();
        validator.ValidateAsync(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(1001).Returns(reviewer);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        photoProfileRepository
            .UpdateAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository
            .GetByUserAndProfileAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns(existingReview);
        reviewRepository
            .Update(Arg.Any<Review>())
            .Returns(Task.CompletedTask);

        var photoRatedProducer = Substitute.For<IKafkaPhotoRatedProducer<PhotoRatedEvent>>();
        var createReviewPublisher = Substitute.For<ICreateReviewEventPublisher>();
        var photoRatingCache = Substitute.For<IPhotoRatingCacheService>();
        var reviewSparksReward = Substitute.For<IReviewSparksRewardService>();
        var ratedProfileSparksReward = Substitute.For<IRatedProfileSparksRewardService>();
        var addLastActiveUser = Substitute.For<IAddLastActiveUser>();

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            photoRatedProducer,
            createReviewPublisher,
            new RankService(),
            photoRatingCache,
            reviewSparksReward,
            ratedProfileSparksReward,
            addLastActiveUser,
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(1001, profile.Id, 8),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await createReviewPublisher.DidNotReceive().PublishAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReviewCreated_AwardsSparksToReviewerAndRatedProfileOwner()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new LooksRatingDbContext(options);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1001,
            TelegramUsername = "reviewer",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable,
        };
        var ownerId = Guid.NewGuid();
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            User = new User
            {
                Id = ownerId,
                TelegramId = 2002,
                TelegramUsername = "owner",
                Name = "Owner",
                Status = VipStatus.Unavaillable,
            },
            SeasonId = Guid.NewGuid(),
            Rating = 7m,
            RatingCount = 1,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };

        var validator = Substitute.For<ICreateReviewValidator>();
        validator.ValidateAsync(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(1001).Returns(reviewer);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        photoProfileRepository
            .UpdateAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository
            .GetByUserAndProfileAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns((Review?)null);
        reviewRepository
            .Create(Arg.Any<Review>())
            .Returns(Task.CompletedTask);

        var photoRatedProducer = Substitute.For<IKafkaPhotoRatedProducer<PhotoRatedEvent>>();
        var createReviewPublisher = Substitute.For<ICreateReviewEventPublisher>();
        createReviewPublisher
            .PublishAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new CreateReviewEvent(reviewer.Id, profile.Id, 1)));

        var photoRatingCache = Substitute.For<IPhotoRatingCacheService>();
        var reviewSparksReward = Substitute.For<IReviewSparksRewardService>();
        var ratedProfileSparksReward = Substitute.For<IRatedProfileSparksRewardService>();
        var addLastActiveUser = Substitute.For<IAddLastActiveUser>();

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            photoRatedProducer,
            createReviewPublisher,
            new RankService(),
            photoRatingCache,
            reviewSparksReward,
            ratedProfileSparksReward,
            addLastActiveUser,
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(1001, profile.Id, 8),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await reviewSparksReward.Received(1).TryAwardForReviewAsync(
            reviewer.TelegramId,
            reviewer.Id,
            Arg.Any<CancellationToken>());
        await ratedProfileSparksReward.Received(1).TryAwardForRatedProfileAsync(
            profile.User.TelegramId,
            profile.UserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCreateReviewEventPublishFails_ReturnsSuccess()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new LooksRatingDbContext(options);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1001,
            TelegramUsername = "reviewer",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable,
        };
        var ownerId = Guid.NewGuid();
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            User = new User
            {
                Id = ownerId,
                TelegramId = 2002,
                TelegramUsername = "owner",
                Name = "Owner",
                Status = VipStatus.Unavaillable,
            },
            SeasonId = Guid.NewGuid(),
            Rating = 7m,
            RatingCount = 1,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };

        var validator = Substitute.For<ICreateReviewValidator>();
        validator.ValidateAsync(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(1001).Returns(reviewer);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        photoProfileRepository
            .UpdateAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository
            .GetByUserAndProfileAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns((Review?)null);
        reviewRepository
            .Create(Arg.Any<Review>())
            .Returns(Task.CompletedTask);

        var photoRatedProducer = Substitute.For<IKafkaPhotoRatedProducer<PhotoRatedEvent>>();
        var createReviewPublisher = Substitute.For<ICreateReviewEventPublisher>();
        createReviewPublisher
            .PublishAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns<Task<CreateReviewEvent>>(_ => throw new InvalidOperationException("kafka down"));

        var photoRatingCache = Substitute.For<IPhotoRatingCacheService>();
        var reviewSparksReward = Substitute.For<IReviewSparksRewardService>();
        var ratedProfileSparksReward = Substitute.For<IRatedProfileSparksRewardService>();
        var addLastActiveUser = Substitute.For<IAddLastActiveUser>();

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            photoRatedProducer,
            createReviewPublisher,
            new RankService(),
            photoRatingCache,
            reviewSparksReward,
            ratedProfileSparksReward,
            addLastActiveUser,
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(1001, profile.Id, 8),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenPhotoRatedKafkaPublishFails_ReturnsSuccess()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new LooksRatingDbContext(options);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1001,
            TelegramUsername = "reviewer",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable,
        };
        var ownerId = Guid.NewGuid();
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            User = new User
            {
                Id = ownerId,
                TelegramId = 2002,
                TelegramUsername = "owner",
                Name = "Owner",
                Status = VipStatus.Unavaillable,
            },
            SeasonId = Guid.NewGuid(),
            Rating = 7m,
            RatingCount = 1,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };

        var validator = Substitute.For<ICreateReviewValidator>();
        validator.ValidateAsync(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(1001).Returns(reviewer);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        photoProfileRepository
            .UpdateAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository
            .GetByUserAndProfileAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns((Review?)null);
        reviewRepository
            .Create(Arg.Any<Review>())
            .Returns(Task.CompletedTask);

        var photoRatedProducer = Substitute.For<IKafkaPhotoRatedProducer<PhotoRatedEvent>>();
        photoRatedProducer
            .ProduceAsync(Arg.Any<PhotoRatedEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("kafka down"));

        var createReviewPublisher = Substitute.For<ICreateReviewEventPublisher>();
        createReviewPublisher
            .PublishAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new CreateReviewEvent(reviewer.Id, profile.Id, 1)));

        var photoRatingCache = Substitute.For<IPhotoRatingCacheService>();
        var reviewSparksReward = Substitute.For<IReviewSparksRewardService>();
        var ratedProfileSparksReward = Substitute.For<IRatedProfileSparksRewardService>();
        var addLastActiveUser = Substitute.For<IAddLastActiveUser>();

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            photoRatedProducer,
            createReviewPublisher,
            new RankService(),
            photoRatingCache,
            reviewSparksReward,
            ratedProfileSparksReward,
            addLastActiveUser,
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(1001, profile.Id, 8),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDuplicateReviewInsertRace_ReturnsReviewAlreadyExists()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new LooksRatingDbContext(options);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1001,
            TelegramUsername = "reviewer",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable,
        };
        var ownerId = Guid.NewGuid();
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            User = new User
            {
                Id = ownerId,
                TelegramId = 2002,
                TelegramUsername = "owner",
                Name = "Owner",
                Status = VipStatus.Unavaillable,
            },
            SeasonId = Guid.NewGuid(),
            Rating = 7m,
            RatingCount = 1,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };

        var validator = Substitute.For<ICreateReviewValidator>();
        validator.ValidateAsync(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(1001).Returns(reviewer);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        photoProfileRepository
            .UpdateAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository
            .GetByUserAndProfileAsync(reviewer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns((Review?)null);
        reviewRepository
            .Create(Arg.Any<Review>())
            .Returns<Task>(_ => throw new DbUpdateException("duplicate key value violates unique constraint"));

        var photoRatedProducer = Substitute.For<IKafkaPhotoRatedProducer<PhotoRatedEvent>>();
        var createReviewPublisher = Substitute.For<ICreateReviewEventPublisher>();
        var photoRatingCache = Substitute.For<IPhotoRatingCacheService>();
        var reviewSparksReward = Substitute.For<IReviewSparksRewardService>();
        var ratedProfileSparksReward = Substitute.For<IRatedProfileSparksRewardService>();
        var addLastActiveUser = Substitute.For<IAddLastActiveUser>();

        var handler = new CreateReviewCommandHandler(
            context,
            userRepository,
            photoProfileRepository,
            reviewRepository,
            validator,
            photoRatedProducer,
            createReviewPublisher,
            new RankService(),
            photoRatingCache,
            reviewSparksReward,
            ratedProfileSparksReward,
            addLastActiveUser,
            NullLogger<CreateReviewCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateReviewCommand(1001, profile.Id, 8),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CreateReviewErrors.ReviewAlreadyExists);
    }
}
