namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public sealed class ReviewSequenceCalculator : IReviewSequenceCalculator
    {
        public int CalculateNextReviewsCount(int? previousReviewsCount)
        {
            if (previousReviewsCount is null or < 1)
            {
                return 1;
            }

            if (previousReviewsCount >= ReviewSequenceConstants.MaxReviewsCount)
            {
                return 1;
            }

            return previousReviewsCount.Value + 1;
        }
    }
}
