using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;

namespace LooksRatingApi.Services.SparksLedger
{
    public class CurrencySparksService : ICurrencySparksService
    {
        private readonly IKafkaEventProducer<CurrencySparksEvent> _producer;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly ISparksWalletProvisioner _sparksWalletProvisioner;

        public CurrencySparksService(
            IKafkaEventProducer<CurrencySparksEvent> producer,
            ISparksLedgerRepository sparksLedgerRepository,
            IEventStoreRepository eventStoreRepository,
            ISparksWalletProvisioner sparksWalletProvisioner)
        {
            _producer = producer;
            _sparksLedgerRepository = sparksLedgerRepository;
            _eventStoreRepository = eventStoreRepository;
            _sparksWalletProvisioner = sparksWalletProvisioner;
        }

        public async Task Credited(Guid userId, decimal creditedSparks, CancellationToken cancellationToken)
        {
            await _sparksWalletProvisioner.EnsureForUserAsync(userId, cancellationToken);

            var sparks = await _sparksLedgerRepository.GetSparksByUserId(userId, cancellationToken);
            if (sparks is null)
            {
                throw new InvalidOperationException($"Sparks wallet not found for user {userId}");
            }

            sparks.AddSparksCount(creditedSparks);
            var domainEvent = new CurrencySparksEvent(sparks.Id, sparks.SparksCount);
            await _eventStoreRepository.SaveEventsAsync(domainEvent.AggregateId, new List<DomainEvent>{domainEvent});
            await _producer.Produce(domainEvent, cancellationToken);
        }
    }
}