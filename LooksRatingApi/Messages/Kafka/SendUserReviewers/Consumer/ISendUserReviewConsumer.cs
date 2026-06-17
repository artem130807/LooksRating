using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.Consumer
{
    public interface ISendUserReviewConsumer<TMessage> : IDisposable where TMessage : DomainEvent
    {
        Task ProcessEvent(TMessage message, CancellationToken cancellationToken);

        Task ReadEvents(CancellationToken cancellationToken);
    }
}