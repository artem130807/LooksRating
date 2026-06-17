using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using LooksRatingApi.Domain.Base;
using Microsoft.Extensions.Options;
using Npgsql.Replication.PgOutput.Messages;
using Quartz.Xml.JobSchedulingData20;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer
{
    public class KafkaEventProducer<TMessage> : IKafkaEventProducer<TMessage> where TMessage:DomainEvent
    {
        private readonly IProducer<string, TMessage> producer;
        private readonly string _topic;
        public KafkaEventProducer(IOptions<KafkaEventProducerSettings> options)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = options.Value.BootstrapServers
            };
            producer = new ProducerBuilder<string, TMessage>(config).SetValueSerializer(new KafkaJsonSerializer<TMessage>())
            .Build();
            var typeName = typeof(TMessage).Name;
            if(!options.Value.Topics.TryGetValue(typeName, out _topic))
            {
                throw new InvalidOperationException($"");
            }
        }
        public void Dispose()
        {
            producer?.Dispose();
        }

        public async Task Produce(TMessage message ,CancellationToken cancellationToken)
        {
            await producer.ProduceAsync(_topic, new Message<string, TMessage>()
            {
               Key = Guid.NewGuid().ToString(),
               Value = message 
            }, cancellationToken);
        }
    }
}