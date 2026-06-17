namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public sealed class ReviewSequenceService : IReviewSequenceService
    {
        private readonly IReviewSequenceStore _sequenceStore;
        private readonly IReviewSequenceCalculator _sequenceCalculator;

        public ReviewSequenceService(
            IReviewSequenceStore sequenceStore,
            IReviewSequenceCalculator sequenceCalculator)
        {
            _sequenceStore = sequenceStore;
            _sequenceCalculator = sequenceCalculator;
        }

        public int ResolveNextReviewsCount(ReviewSequenceKey key) =>
            _sequenceStore.ResolveNextReviewsCount(key, _sequenceCalculator.CalculateNextReviewsCount);
    }
}
