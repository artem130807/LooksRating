using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.Base;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Producers
{
    public class KafkaPhotoRatedProducer<TMessage> : IKafkaPhotoRatedProducer<TMessage> where TMessage:DomainEvent
    {
        private readonly IProducer<string, TMessage> producer;
        private readonly string _topic;
        public KafkaPhotoRatedProducer(IOptions<KafkaProducerSettings> options)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                BrokerAddressFamily = BrokerAddressFamily.V4
            };
            producer = new ProducerBuilder<string, TMessage>(config).SetValueSerializer(new KafkaJsonSerializer<TMessage>())
            .Build();

            var typeName = typeof(TMessage).Name;
            if (!options.Value.Topics.TryGetValue(typeName, out _topic))
            {
                throw new InvalidOperationException(
                $"No topic configured for type {typeName}. " +
                $"Available topics: {string.Join(", ", options.Value.Topics.Keys)}");
            }
        }
        public void Dispose()
        {
            producer?.Dispose();
        }

        public async Task ProduceAsync(TMessage message, CancellationToken cancellationToken)
        {
            await producer.ProduceAsync(_topic, new Message<string, TMessage>()
            {
                Key = Guid.NewGuid().ToString(),
                Value = message 
            }, cancellationToken);
        }
    }
}