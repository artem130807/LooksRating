using Confluent.Kafka;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Consumers
{
    public class KafkaPhotoRatedConsumer<TMessage> : IKafkaPhotoRatedConsumer<TMessage> where TMessage : DomainEvent
    {
        private readonly string _topic;
        private readonly IConsumer<string, TMessage> _consumer;
        private readonly ILogger<KafkaPhotoRatedConsumer<TMessage>> _logger;
        private readonly IPhotoRatingCacheService _photoRatingCacheService;

        public KafkaPhotoRatedConsumer(
            IOptions<KafkaConsumerSettings> options,
            ILogger<KafkaPhotoRatedConsumer<TMessage>> logger,
            IPhotoRatingCacheService photoRatingCacheService)
        {
            _logger = logger;
            _photoRatingCacheService = photoRatingCacheService;
            var config = new ConsumerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                GroupId = options.Value.GroupId,
                BrokerAddressFamily = BrokerAddressFamily.V4,
                AllowAutoCreateTopics = true
            };
            _topic = options.Value.Topic;
            _consumer = new ConsumerBuilder<string, TMessage>(config)
                .SetValueDeserializer(new KafkaValueDeserializer<TMessage>())
                .Build();
            _consumer.Subscribe(_topic);
            _logger.LogInformation("Подписался на топик {Topic}", _topic);
        }

        public void Dispose()
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }

        public async Task ReadEvents(CancellationToken cancellationToken)
        {
            var timeout = TimeSpan.FromSeconds(5);
            try
            {
                var consumeResult = _consumer.Consume(timeout);
                if (consumeResult?.Message?.Value is PhotoRatedEvent @event)
                {
                    await _photoRatingCacheService.SyncPhotoRatingAsync(@event, cancellationToken);
                    _consumer.Commit();
                }
            }
            catch (ConsumeException ex)
            {
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("Топик {Topic} пока недоступен: {Reason}", _topic, ex.Error.Reason);
                    return;
                }

                _logger.LogError(ex, "Error reading events");
                throw;
            }
        }
    }
}
