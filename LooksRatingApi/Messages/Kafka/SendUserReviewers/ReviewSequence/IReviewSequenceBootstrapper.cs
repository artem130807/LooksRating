namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public interface IReviewSequenceBootstrapper
    {
        Task BootstrapAsync(CancellationToken cancellationToken);
    }
}
