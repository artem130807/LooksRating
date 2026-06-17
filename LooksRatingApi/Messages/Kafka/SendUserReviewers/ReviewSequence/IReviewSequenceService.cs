namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public interface IReviewSequenceService
    {
        int ResolveNextReviewsCount(ReviewSequenceKey key);
    }
}
