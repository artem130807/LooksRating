using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Consumers.EventConsumer
{
    public interface IKafkaEventConsumer<TMessage> : IDisposable where TMessage : DomainEvent
    {
        Task ProcessEvent(TMessage message, CancellationToken cancellationToken);

        Task ReadEvents(CancellationToken cancellationToken);
    }
}