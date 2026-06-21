using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SparksLedger;

namespace LooksRatingApi.Services.SparksLedger
{
    public class CurrencyDebitedService : ICurrencyDebitedService
    {
        private readonly IKafkaEventProducer<CurrencyDebitedEvent> _producer;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly IUserRepository _userRepository;
        public CurrencyDebitedService(IKafkaEventProducer<CurrencyDebitedEvent> producer, ISparksLedgerRepository sparksLedgerRepository, IEventStoreRepository eventStoreRepository, IUserRepository userRepository)
        {
            _producer = producer;
            _sparksLedgerRepository = sparksLedgerRepository;
            _eventStoreRepository = eventStoreRepository;
            _userRepository = userRepository;
        }
        public async Task<Guid> Debited(Guid userId, decimal debitedSparks, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user is null)
            {
                throw new SparksLedgerOperationException("Пользователь не найден");
            }

            var sparks = await _sparksLedgerRepository.GetSparksByUserId(user.Id, cancellationToken);
            if (sparks is null)
            {
                throw new SparksLedgerOperationException("Кошелёк искр не найден");
            }

            sparks.WritingOffSparks(debitedSparks);
            var domainEvent = new CurrencyDebitedEvent(sparks.Id, sparks.SparksCount);
            await _eventStoreRepository.SaveEventsAsync(domainEvent.AggregateId, new List<DomainEvent>{domainEvent});
            await _producer.Produce(domainEvent, cancellationToken);
            return domainEvent.EventId;
        }
    }
}