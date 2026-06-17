using Confluent.Kafka;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Consumers
{
    public sealed class KafkaPhotoRatedConsumer<TMessage> : IKafkaPhotoRatedConsumer<TMessage> where TMessage : DomainEvent
    {
        private readonly KafkaConsumerSettings _settings;
        private readonly ILogger<KafkaPhotoRatedConsumer<TMessage>> _logger;
        private readonly IPhotoRatingCacheService _photoRatingCacheService;
        private readonly object _consumerLock = new();
        private IConsumer<string, TMessage>? _consumer;
        private bool _disposed;
        private bool _configurationValid;

        public KafkaPhotoRatedConsumer(
            IOptions<KafkaConsumerSettings> options,
            ILogger<KafkaPhotoRatedConsumer<TMessage>> logger,
            IPhotoRatingCacheService photoRatingCacheService)
        {
            _settings = options.Value ?? new KafkaConsumerSettings();
            _logger = logger;
            _photoRatingCacheService = photoRatingCacheService;
            _configurationValid = !string.IsNullOrWhiteSpace(_settings.BootstrapServers)
                && !string.IsNullOrWhiteSpace(_settings.Topic)
                && !string.IsNullOrWhiteSpace(_settings.GroupId);

            if (!_configurationValid)
            {
                _logger.LogWarning("PhotoRated Kafka consumer is not configured");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            lock (_consumerLock)
            {
                _consumer?.Close();
                _consumer?.Dispose();
                _consumer = null;
            }
        }

        public async Task ReadEvents(CancellationToken cancellationToken)
        {
            if (!_configurationValid)
            {
                return;
            }

            var consumer = GetOrCreateConsumer();
            if (consumer is null)
            {
                return;
            }

            var timeout = TimeSpan.FromSeconds(5);
            try
            {
                var consumeResult = consumer.Consume(timeout);
                if (consumeResult?.Message?.Value is PhotoRatedEvent @event)
                {
                    await _photoRatingCacheService.SyncPhotoRatingAsync(@event, cancellationToken);
                    consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("Топик {Topic} пока недоступен: {Reason}", _settings.Topic, ex.Error.Reason);
                    return;
                }

                _logger.LogError(ex, "Error reading photo rated events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected photo rated Kafka consumer error");
            }
        }

        private IConsumer<string, TMessage>? GetOrCreateConsumer()
        {
            if (_consumer is not null)
            {
                return _consumer;
            }

            lock (_consumerLock)
            {
                if (_consumer is not null)
                {
                    return _consumer;
                }

                try
                {
                    var config = new ConsumerConfig
                    {
                        BootstrapServers = _settings.BootstrapServers,
                        GroupId = _settings.GroupId,
                        BrokerAddressFamily = BrokerAddressFamily.V4,
                        AllowAutoCreateTopics = true
                    };

                    _consumer = new ConsumerBuilder<string, TMessage>(config)
                        .SetValueDeserializer(new KafkaValueDeserializer<TMessage>())
                        .Build();
                    _consumer.Subscribe(_settings.Topic);
                    _logger.LogInformation("Подписался на топик {Topic}", _settings.Topic);
                    return _consumer;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize PhotoRated Kafka consumer");
                    _configurationValid = false;
                    return null;
                }
            }
        }
    }
}
