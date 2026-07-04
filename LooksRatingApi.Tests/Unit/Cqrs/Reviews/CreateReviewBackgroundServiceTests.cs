using Hangfire;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Cqrs.Reviews;

public sealed class CreateReviewBackgroundServiceTests
{
    [Fact]
    public async Task ProcessOutboxAsync_WhenNewReview_CompletesOutboxAndExecutesSideEffects()
    {
        await using var fixture = await CreateFixtureAsync();
        var (outbox, payload, _) = CreateOutbox(isNewReview: true);
        fixture.Context.OutboxMessages.Add(outbox);
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.ProcessOutboxAsync(outbox.Id, CancellationToken.None);

        await fixture.PhotoRatingCache.Received(1).MarkProfileAsRatedAsync(
            payload.ReviewerUserId,
            payload.SeasonId,
            payload.PhotoProfileId,
            CancellationToken.None);
        await fixture.PhotoRatedProducer.Received(1).ProduceAsync(Arg.Any<PhotoRatedEvent>(), CancellationToken.None);
        await fixture.CreateReviewPublisher.Received(1)
            .PublishAsync(payload.ReviewerUserId, payload.PhotoProfileId, CancellationToken.None);
        await fixture.ReviewSparksReward.Received(1)
            .TryAwardForReviewAsync(payload.ReviewerTelegramId, payload.ReviewerUserId, CancellationToken.None);
        await fixture.RatedProfileSparksReward.Received(1)
            .TryAwardForRatedProfileAsync(payload.ProfileOwnerTelegramId!.Value, payload.ProfileOwnerUserId, CancellationToken.None);
        await fixture.AddLastActiveUser.Received(1).Add(payload.ReviewerUserId, payload.ReviewerTelegramId);

        var stored = await fixture.Context.OutboxMessages.SingleAsync(x => x.Id == outbox.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Completed);
        stored.TryReadState<CreateReviewOutboxState>(out var state).Should().BeTrue();
        state!.CacheSynced.Should().BeTrue();
        state.PhotoRatedEventPublished.Should().BeTrue();
        state.CreateReviewEventPublished.Should().BeTrue();
        state.ReviewerRewardGranted.Should().BeTrue();
        state.ProfileRewardGranted.Should().BeTrue();
        state.LastActiveUpdated.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessOutboxAsync_WhenAlreadyCompleted_IsIdempotent()
    {
        await using var fixture = await CreateFixtureAsync();
        var (outbox, payload, _) = CreateOutbox(isNewReview: true);
        fixture.Context.OutboxMessages.Add(outbox);
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.ProcessOutboxAsync(outbox.Id, CancellationToken.None);
        await fixture.Service.ProcessOutboxAsync(outbox.Id, CancellationToken.None);

        await fixture.PhotoRatedProducer.Received(1).ProduceAsync(Arg.Any<PhotoRatedEvent>(), CancellationToken.None);
        await fixture.CreateReviewPublisher.Received(1)
            .PublishAsync(payload.ReviewerUserId, payload.PhotoProfileId, CancellationToken.None);
    }

    [Fact]
    public async Task ProcessOutboxAsync_WhenIntermediateStepFails_ResumesFromSavedProgress()
    {
        await using var fixture = await CreateFixtureAsync();
        var (outbox, payload, _) = CreateOutbox(isNewReview: true);
        fixture.Context.OutboxMessages.Add(outbox);
        await fixture.Context.SaveChangesAsync();

        var producerCalls = 0;
        fixture.PhotoRatedProducer
            .ProduceAsync(Arg.Any<PhotoRatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                producerCalls++;
                if (producerCalls == 1)
                {
                    throw new InvalidOperationException("kafka unavailable");
                }

                return Task.CompletedTask;
            });

        await fixture.Service.ProcessOutboxAsync(outbox.Id, CancellationToken.None);

        var failed = await fixture.Context.OutboxMessages.SingleAsync(x => x.Id == outbox.Id);
        failed.Status.Should().Be(OutboxMessageStatus.Failed);
        failed.TryReadState<CreateReviewOutboxState>(out var failedState).Should().BeTrue();
        failedState!.CacheSynced.Should().BeTrue();
        failedState.PhotoRatedEventPublished.Should().BeFalse();
        failed.NextAttemptAt.Should().NotBeNull();

        // Force immediate retry in unit test.
        await fixture.Context.OutboxMessages
            .Where(x => x.Id == outbox.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)));

        await fixture.Service.ProcessOutboxAsync(outbox.Id, CancellationToken.None);

        var completed = await fixture.Context.OutboxMessages.SingleAsync(x => x.Id == outbox.Id);
        completed.Status.Should().Be(OutboxMessageStatus.Completed);
        await fixture.PhotoRatingCache.Received(1).MarkProfileAsRatedAsync(
            payload.ReviewerUserId,
            payload.SeasonId,
            payload.PhotoProfileId,
            CancellationToken.None);
        await fixture.PhotoRatedProducer.Received(2).ProduceAsync(Arg.Any<PhotoRatedEvent>(), CancellationToken.None);
    }

    [Fact]
    public async Task EnqueuePendingOutboxAsync_WhenDueItemsExist_EnqueuesHangfireJobs()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Context.OutboxMessages.AddRange(
            CreateOutbox(isNewReview: true).Outbox,
            CreateOutbox(isNewReview: false).Outbox);
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.EnqueuePendingOutboxAsync(CancellationToken.None);

        fixture.BackgroundJobClient.Received(2).Enqueue<IReviewBackgroundService>(
            Arg.Any<System.Linq.Expressions.Expression<Action<IReviewBackgroundService>>>());
    }

    [Fact]
    public async Task ProcessOutboxAsync_WhenMessageTypeIsNotSupported_DoesNotClaimOrMutateMessage()
    {
        await using var fixture = await CreateFixtureAsync();
        var otherOutbox = OutboxMessage.Create(
            "SomeOtherMessageType.v1",
            new { A = 1 },
            new { Done = false });
        fixture.Context.OutboxMessages.Add(otherOutbox);
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.ProcessOutboxAsync(otherOutbox.Id, CancellationToken.None);

        var stored = await fixture.Context.OutboxMessages.SingleAsync(x => x.Id == otherOutbox.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Pending);
        stored.Attempts.Should().Be(0);
    }

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new LooksRatingDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        var photoRatedProducer = Substitute.For<IKafkaPhotoRatedProducer<PhotoRatedEvent>>();
        var reviewSparksReward = Substitute.For<IReviewSparksRewardService>();
        reviewSparksReward
            .TryAwardForReviewAsync(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var ratedProfileSparksReward = Substitute.For<IRatedProfileSparksRewardService>();
        ratedProfileSparksReward
            .TryAwardForRatedProfileAsync(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var photoRatingCache = Substitute.For<IPhotoRatingCacheService>();
        var createReviewPublisher = Substitute.For<ICreateReviewEventPublisher>();
        var addLastActiveUser = Substitute.For<IAddLastActiveUser>();

        var service = new CreateReviewBackgroundService(
            context,
            backgroundJobClient,
            reviewSparksReward,
            ratedProfileSparksReward,
            photoRatingCache,
            photoRatedProducer,
            createReviewPublisher,
            addLastActiveUser,
            NullLogger<CreateReviewBackgroundService>.Instance);

        return new Fixture(
            context,
            connection,
            service,
            backgroundJobClient,
            photoRatedProducer,
            reviewSparksReward,
            ratedProfileSparksReward,
            photoRatingCache,
            createReviewPublisher,
            addLastActiveUser);
    }

    private static (OutboxMessage Outbox, CreateReviewOutboxPayload Payload, CreateReviewOutboxState State) CreateOutbox(bool isNewReview)
    {
        var reviewId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var payload = new CreateReviewOutboxPayload
        {
            ReviewId = reviewId,
            ReviewerUserId = reviewerId,
            ReviewerTelegramId = 7001,
            PhotoProfileId = profileId,
            SeasonId = seasonId,
            IsNewReview = isNewReview,
            UpdatedProfileRating = 8.5m,
            UpdatedProfileRatingCount = 10,
            ProfileCity = "moscow",
            ProfileOwnerUserId = ownerId,
            ProfileOwnerTelegramId = 7002
        };
        var state = CreateReviewOutboxState.Initial(isNewReview);
        var outbox = OutboxMessage.Create(
            CreateReviewOutboxMessage.Type,
            payload,
            state);

        return (outbox, payload, state);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture(
            LooksRatingDbContext context,
            SqliteConnection connection,
            CreateReviewBackgroundService service,
            IBackgroundJobClient backgroundJobClient,
            IKafkaPhotoRatedProducer<PhotoRatedEvent> photoRatedProducer,
            IReviewSparksRewardService reviewSparksReward,
            IRatedProfileSparksRewardService ratedProfileSparksReward,
            IPhotoRatingCacheService photoRatingCache,
            ICreateReviewEventPublisher createReviewPublisher,
            IAddLastActiveUser addLastActiveUser)
        {
            Context = context;
            Connection = connection;
            Service = service;
            BackgroundJobClient = backgroundJobClient;
            PhotoRatedProducer = photoRatedProducer;
            ReviewSparksReward = reviewSparksReward;
            RatedProfileSparksReward = ratedProfileSparksReward;
            PhotoRatingCache = photoRatingCache;
            CreateReviewPublisher = createReviewPublisher;
            AddLastActiveUser = addLastActiveUser;
        }

        public LooksRatingDbContext Context { get; }
        public SqliteConnection Connection { get; }
        public CreateReviewBackgroundService Service { get; }
        public IBackgroundJobClient BackgroundJobClient { get; }
        public IKafkaPhotoRatedProducer<PhotoRatedEvent> PhotoRatedProducer { get; }
        public IReviewSparksRewardService ReviewSparksReward { get; }
        public IRatedProfileSparksRewardService RatedProfileSparksReward { get; }
        public IPhotoRatingCacheService PhotoRatingCache { get; }
        public ICreateReviewEventPublisher CreateReviewPublisher { get; }
        public IAddLastActiveUser AddLastActiveUser { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
