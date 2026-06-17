using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer
{
    public interface IKafkaEventProducer<TMessage>:IDisposable  where TMessage: DomainEvent
    {
        Task Produce(TMessage message, CancellationToken cancellationToken);
    }
}