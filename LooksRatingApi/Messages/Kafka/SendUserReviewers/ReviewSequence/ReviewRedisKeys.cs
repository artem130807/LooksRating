namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public static class ReviewRedisKeys
    {
        public static string SequenceCount(Guid photoProfileId) =>
            $"review:sequence:profile:{photoProfileId:N}";
    }
}
