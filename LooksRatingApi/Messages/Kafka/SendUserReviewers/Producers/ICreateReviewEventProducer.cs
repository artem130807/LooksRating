using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.Producers
{
    public interface ICreateReviewEventProducer : IDisposable
    {
        Task ProduceAsync(CreateReviewEvent message, CancellationToken cancellationToken);
    }
}
