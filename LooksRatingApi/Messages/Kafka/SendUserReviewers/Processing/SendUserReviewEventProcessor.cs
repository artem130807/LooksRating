using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.Processing
{
    public sealed class SendUserReviewEventProcessor : ISendUserReviewEventProcessor
    {
        private readonly IReviewSequenceStore _sequenceStore;
        private readonly IReviewSequenceService _reviewSequenceService;
        private readonly IReviewMilestoneNotifier _milestoneNotifier;
        private readonly ILogger<SendUserReviewEventProcessor> _logger;

        public SendUserReviewEventProcessor(
            IReviewSequenceStore sequenceStore,
            IReviewSequenceService reviewSequenceService,
            IReviewMilestoneNotifier milestoneNotifier,
            ILogger<SendUserReviewEventProcessor> logger)
        {
            _sequenceStore = sequenceStore;
            _reviewSequenceService = reviewSequenceService;
            _milestoneNotifier = milestoneNotifier;
            _logger = logger;
        }

        public async Task<CreateReviewEvent> ProcessAsync(CreateReviewEvent incoming, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sequenceKey = ReviewSequenceKey.From(incoming);
            if (incoming.ReviewerId == Guid.Empty)
            {
                incoming.ReviewerId = incoming.AggregateId;
            }

            if (incoming.ReviewsCount >= 1)
            {
                _sequenceStore.SetLastReviewsCount(sequenceKey, incoming.ReviewsCount);
                _logger.LogDebug(
                    "CreateReviewEvent already enriched: reviewer={ReviewerId}, photoProfile={PhotoProfileId}, reviewsCount={ReviewsCount}",
                    incoming.ReviewerId,
                    sequenceKey.PhotoProfileId,
                    incoming.ReviewsCount);
            }
            else
            {
                var nextCount = _reviewSequenceService.ResolveNextReviewsCount(sequenceKey);
                incoming.ReviewsCount = nextCount;

                _logger.LogInformation(
                    "CreateReviewEvent enriched: reviewer={ReviewerId}, photoProfile={PhotoProfileId}, next={NextCount}",
                    incoming.ReviewerId,
                    sequenceKey.PhotoProfileId,
                    nextCount);
            }

            if (incoming.ReviewsCount == ReviewSequenceConstants.MaxReviewsCount)
            {
                await _milestoneNotifier.TryNotifyAsync(incoming, cancellationToken);
            }

            return incoming;
        }
    }
}
