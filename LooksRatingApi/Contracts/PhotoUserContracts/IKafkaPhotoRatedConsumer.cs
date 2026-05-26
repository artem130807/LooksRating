using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IKafkaPhotoRatedConsumer<TMessage>:IDisposable where TMessage: DomainEvent
    {
        Task ReadEvents(CancellationToken cancellationToken);
    }
}