namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public interface IReviewSequenceStore
    {
        int? GetLastReviewsCount(ReviewSequenceKey key);

        void SetLastReviewsCount(ReviewSequenceKey key, int reviewsCount);

        int ResolveNextReviewsCount(ReviewSequenceKey key, Func<int?, int> calculateNext);
    }
}
