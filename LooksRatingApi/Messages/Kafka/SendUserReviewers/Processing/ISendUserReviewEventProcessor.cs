using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.Processing
{
    public interface ISendUserReviewEventProcessor
    {
        Task<CreateReviewEvent> ProcessAsync(CreateReviewEvent incoming, CancellationToken cancellationToken);
    }
}
