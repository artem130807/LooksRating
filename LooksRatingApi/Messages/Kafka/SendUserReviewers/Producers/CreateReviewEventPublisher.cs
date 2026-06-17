using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.Producers
{
    public sealed class CreateReviewEventPublisher : ICreateReviewEventPublisher
    {
        private readonly IReviewSequenceService _reviewSequenceService;
        private readonly ICreateReviewEventProducer _producer;
        private readonly ILogger<CreateReviewEventPublisher> _logger;

        public CreateReviewEventPublisher(
            IReviewSequenceService reviewSequenceService,
            ICreateReviewEventProducer producer,
            ILogger<CreateReviewEventPublisher> logger)
        {
            _reviewSequenceService = reviewSequenceService;
            _producer = producer;
            _logger = logger;
        }

        public async Task<CreateReviewEvent> PublishAsync(
            Guid reviewerId,
            Guid photoProfileId,
            CancellationToken cancellationToken)
        {
            var sequenceKey = new ReviewSequenceKey(photoProfileId);
            var reviewsCount = _reviewSequenceService.ResolveNextReviewsCount(sequenceKey);

            var reviewEvent = new CreateReviewEvent(
                reviewerId,
                photoProfileId,
                reviewsCount,
                isNewReview: true);
            await _producer.ProduceAsync(reviewEvent, cancellationToken);

            _logger.LogInformation(
                "CreateReviewEvent published: reviewer={ReviewerId}, photoProfile={PhotoProfileId}, reviewsCount={ReviewsCount}",
                reviewerId,
                photoProfileId,
                reviewsCount);

            return reviewEvent;
        }
    }
}
