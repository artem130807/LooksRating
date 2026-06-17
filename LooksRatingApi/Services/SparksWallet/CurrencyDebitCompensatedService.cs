using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;

namespace LooksRatingApi.Services.SparksLedger
{
    public sealed class CurrencyDebitCompensatedService : ICurrencyDebitCompensatedService
    {
        private readonly IKafkaEventProducer<CurrencyDebitCompensatedEvent> _producer;
        private readonly ISparksLedgerEventDispatcher _eventDispatcher;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        private readonly IEventStoreRepository _eventStoreRepository;

        public CurrencyDebitCompensatedService(
            IKafkaEventProducer<CurrencyDebitCompensatedEvent> producer,
            ISparksLedgerEventDispatcher eventDispatcher,
            ISparksLedgerRepository sparksLedgerRepository,
            IEventStoreRepository eventStoreRepository)
        {
            _producer = producer;
            _eventDispatcher = eventDispatcher;
            _sparksLedgerRepository = sparksLedgerRepository;
            _eventStoreRepository = eventStoreRepository;
        }

        public async Task Compensate(
            Guid userId,
            decimal compensatedAmount,
            Guid originalEventId,
            string reason,
            CancellationToken cancellationToken)
        {
            var sparks = await _sparksLedgerRepository.GetSparksByUserId(userId, cancellationToken);
            if (sparks is null)
            {
                return;
            }

            sparks.AddSparksCount(compensatedAmount);
            var domainEvent = new CurrencyDebitCompensatedEvent(
                sparks.Id,
                sparks.SparksCount,
                compensatedAmount,
                originalEventId,
                reason);

            await _eventStoreRepository.SaveEventsAsync(domainEvent.AggregateId, new List<DomainEvent> { domainEvent });
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            await _producer.Produce(domainEvent, cancellationToken);
        }
    }
}
