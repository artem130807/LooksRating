using Hangfire;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed class CreateReviewBackgroundService : IReviewBackgroundService
    {
        private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);
        private const int DispatcherBatchSize = 200;

        private readonly LooksRatingDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IKafkaPhotoRatedProducer<PhotoRatedEvent> _producer;
        private readonly IReviewSparksRewardService _reviewSparksRewardService;
        private readonly IRatedProfileSparksRewardService _ratedProfileSparksRewardService;
        private readonly IPhotoRatingCacheService _photoRatingCacheService;
        private readonly ICreateReviewEventPublisher _createReviewEventPublisher;
        private readonly IAddLastActiveUser _addLastActiveUser;
        private readonly ILogger<CreateReviewBackgroundService> _logger;

        public CreateReviewBackgroundService(
            LooksRatingDbContext context,
            IBackgroundJobClient backgroundJobClient,
            IReviewSparksRewardService reviewSparksRewardService,
            IRatedProfileSparksRewardService ratedProfileSparksRewardService,
            IPhotoRatingCacheService photoRatingCacheService,
            IKafkaPhotoRatedProducer<PhotoRatedEvent> producer,
            ICreateReviewEventPublisher createReviewEventPublisher,
            IAddLastActiveUser addLastActiveUser,
            ILogger<CreateReviewBackgroundService> logger)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _reviewSparksRewardService = reviewSparksRewardService;
            _ratedProfileSparksRewardService = ratedProfileSparksRewardService;
            _photoRatingCacheService = photoRatingCacheService;
            _producer = producer;
            _createReviewEventPublisher = createReviewEventPublisher;
            _addLastActiveUser = addLastActiveUser;
            _logger = logger;
        }

        [Queue("create-review")]
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessOutboxAsync(Guid outboxId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var claimed = await TryClaimOutboxAsync(outboxId, now, cancellationToken);
            if (!claimed)
            {
                return;
            }

            var outbox = await _context.OutboxMessages
                .FirstOrDefaultAsync(x => x.Id == outboxId, cancellationToken);
            if (outbox is null)
            {
                return;
            }

            if (!string.Equals(outbox.MessageType, CreateReviewOutboxMessage.Type, StringComparison.Ordinal))
            {
                await MarkOutboxFailedAsync(
                    outbox,
                    "Unexpected outbox message type",
                    new InvalidOperationException($"Message type '{outbox.MessageType}' is not handled by create-review processor"),
                    cancellationToken);
                return;
            }

            if (!outbox.TryReadPayload<CreateReviewOutboxPayload>(out var payload) || payload is null)
            {
                await MarkOutboxFailedAsync(
                    outbox,
                    "Payload deserialize failed",
                    new InvalidOperationException("Unable to deserialize CreateReviewOutboxPayload"),
                    cancellationToken);
                return;
            }

            if (!outbox.TryReadState<CreateReviewOutboxState>(out var state) || state is null)
            {
                await MarkOutboxFailedAsync(
                    outbox,
                    "State deserialize failed",
                    new InvalidOperationException("Unable to deserialize CreateReviewOutboxState"),
                    cancellationToken);
                return;
            }

            var photoRatedEvent = new PhotoRatedEvent(
                payload.PhotoProfileId,
                payload.UpdatedProfileRating,
                payload.UpdatedProfileRatingCount,
                payload.ProfileCity,
                payload.SeasonId);

            if (!state.CacheSynced)
            {
                try
                {
                    await _photoRatingCacheService.MarkProfileAsRatedAsync(
                        payload.ReviewerUserId,
                        payload.SeasonId,
                        payload.PhotoProfileId,
                        cancellationToken);
                    await _photoRatingCacheService.SyncPhotoRatingAsync(photoRatedEvent, cancellationToken);
                    state = state with { CacheSynced = true };
                    outbox.SetState(state, DateTime.UtcNow);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    await MarkOutboxFailedAsync(outbox, "Cache sync failed", ex, cancellationToken);
                    return;
                }
            }

            if (!state.PhotoRatedEventPublished)
            {
                try
                {
                    await _producer.ProduceAsync(photoRatedEvent, cancellationToken);
                    state = state with { PhotoRatedEventPublished = true };
                    outbox.SetState(state, DateTime.UtcNow);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    await MarkOutboxFailedAsync(outbox, "PhotoRatedEvent publish failed", ex, cancellationToken);
                    return;
                }
            }

            if (payload.IsNewReview && !state.CreateReviewEventPublished)
            {
                try
                {
                    await _createReviewEventPublisher.PublishAsync(
                        payload.ReviewerUserId,
                        payload.PhotoProfileId,
                        cancellationToken);
                    state = state with { CreateReviewEventPublished = true };
                    outbox.SetState(state, DateTime.UtcNow);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    await MarkOutboxFailedAsync(outbox, "CreateReviewEvent publish failed", ex, cancellationToken);
                    return;
                }
            }

            if (!state.ReviewerRewardGranted)
            {
                var rewardGranted = await _reviewSparksRewardService.TryAwardForReviewAsync(
                    payload.ReviewerTelegramId,
                    payload.ReviewerUserId,
                    cancellationToken);
                if (!rewardGranted)
                {
                    await MarkOutboxFailedAsync(
                        outbox,
                        "Reviewer sparks reward failed",
                        new InvalidOperationException("Reviewer sparks reward service returned failure"),
                        cancellationToken);
                    return;
                }
                state = state with { ReviewerRewardGranted = true };
                outbox.SetState(state, DateTime.UtcNow);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (!state.ProfileRewardGranted)
            {
                if (payload.ProfileOwnerTelegramId.HasValue && payload.ProfileOwnerUserId != Guid.Empty)
                {
                    var rewardGranted = await _ratedProfileSparksRewardService.TryAwardForRatedProfileAsync(
                        payload.ProfileOwnerTelegramId.Value,
                        payload.ProfileOwnerUserId,
                        cancellationToken);
                    if (!rewardGranted)
                    {
                        await MarkOutboxFailedAsync(
                            outbox,
                            "Profile owner sparks reward failed",
                            new InvalidOperationException("Rated-profile sparks reward service returned failure"),
                            cancellationToken);
                        return;
                    }
                }

                state = state with { ProfileRewardGranted = true };
                outbox.SetState(state, DateTime.UtcNow);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (!state.LastActiveUpdated)
            {
                try
                {
                    await _addLastActiveUser.Add(payload.ReviewerUserId, payload.ReviewerTelegramId);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    _logger.LogWarning(
                        ex,
                        "Failed to update last active timestamp for reviewer {ReviewerId}",
                        payload.ReviewerUserId);
                }

                state = state with { LastActiveUpdated = true };
                outbox.SetState(state, DateTime.UtcNow);
                await _context.SaveChangesAsync(cancellationToken);
            }

            outbox.MarkCompleted(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
        }

        [Queue("create-review")]
        [AutomaticRetry(Attempts = 0)]
        public async Task EnqueuePendingOutboxAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var staleBefore = now.Subtract(ProcessingTimeout);

            var dueOutboxIds = await _context.OutboxMessages
                .AsNoTracking()
                .Where(x =>
                    x.MessageType == CreateReviewOutboxMessage.Type
                    && (
                        x.Status == OutboxMessageStatus.Pending
                        || (x.Status == OutboxMessageStatus.Failed
                        && x.NextAttemptAt.HasValue
                        && x.NextAttemptAt <= now)
                        || (x.Status == OutboxMessageStatus.Processing
                        && x.ProcessingStartedAt.HasValue
                        && x.ProcessingStartedAt <= staleBefore)))
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.Id)
                .Take(DispatcherBatchSize)
                .ToListAsync(cancellationToken);

            foreach (var outboxId in dueOutboxIds)
            {
                _backgroundJobClient.Enqueue<IReviewBackgroundService>(service =>
                    service.ProcessOutboxAsync(outboxId, default(CancellationToken)));
            }
        }

        private async Task<bool> TryClaimOutboxAsync(Guid outboxId, DateTime nowUtc, CancellationToken cancellationToken)
        {
            var staleBefore = nowUtc.Subtract(ProcessingTimeout);
            var claimedRows = await _context.OutboxMessages
                .Where(x =>
                    x.Id == outboxId
                    && x.MessageType == CreateReviewOutboxMessage.Type
                    && (
                        x.Status == OutboxMessageStatus.Pending
                        || (x.Status == OutboxMessageStatus.Failed
                            && x.NextAttemptAt.HasValue
                            && x.NextAttemptAt <= nowUtc)
                        || (x.Status == OutboxMessageStatus.Processing
                            && x.ProcessingStartedAt.HasValue
                            && x.ProcessingStartedAt <= staleBefore)))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, OutboxMessageStatus.Processing)
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                    .SetProperty(x => x.ProcessingStartedAt, nowUtc)
                    .SetProperty(x => x.UpdatedAt, nowUtc),
                    cancellationToken);

            return claimedRows > 0;
        }

        private async Task MarkOutboxFailedAsync(
            OutboxMessage outbox,
            string stage,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var attempts = Math.Max(1, outbox.Attempts);
            var backoffSeconds = Math.Min(600, 10 * (int)Math.Pow(2, Math.Min(attempts - 1, 6)));
            var retryDelay = TimeSpan.FromSeconds(backoffSeconds);
            var error = $"{stage}: {exception.GetType().Name}: {exception.Message}";
            if (error.Length > 2000)
            {
                error = error[..2000];
            }

            outbox.MarkFailed(error, now, retryDelay);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                exception,
                "Create-review outbox failed at stage {Stage} for outbox {OutboxId}, type {MessageType}. Retry in {RetrySeconds}s",
                stage,
                outbox.Id,
                outbox.MessageType,
                backoffSeconds);
        }
    }
}