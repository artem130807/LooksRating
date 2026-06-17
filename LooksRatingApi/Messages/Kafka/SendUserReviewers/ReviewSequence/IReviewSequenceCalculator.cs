namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public interface IReviewSequenceCalculator
    {
        int CalculateNextReviewsCount(int? previousReviewsCount);
    }
}
