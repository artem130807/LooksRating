using Confluent.Kafka;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.Producers
{
    public sealed class KafkaCreateReviewEventProducer : ICreateReviewEventProducer
    {
        private readonly KafkaProducerSettings _settings;
        private readonly ILogger<KafkaCreateReviewEventProducer> _logger;
        private readonly object _sync = new();
        private IProducer<string, CreateReviewEvent>? _producer;
        private string? _topic;
        private bool _disposed;
        private bool _configurationValid;

        public KafkaCreateReviewEventProducer(
            IOptions<KafkaProducerSettings> options,
            ILogger<KafkaCreateReviewEventProducer> logger)
        {
            _settings = options.Value ?? new KafkaProducerSettings();
            _logger = logger;
            _configurationValid = IsConfigurationValid(_settings);

            if (!_configurationValid)
            {
                _logger.LogWarning("CreateReviewEvent Kafka producer is not configured");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            lock (_sync)
            {
                _producer?.Dispose();
                _producer = null;
            }
        }

        public Task ProduceAsync(CreateReviewEvent message, CancellationToken cancellationToken)
        {
            var producer = GetOrCreateProducer();
            if (producer is null || _topic is null)
            {
                _logger.LogWarning("CreateReviewEvent Kafka producer is not configured; event skipped");
                return Task.CompletedTask;
            }

            var key = ReviewSequenceKey.From(message).ToKafkaKey();
            return producer.ProduceAsync(
                _topic,
                new Message<string, CreateReviewEvent>
                {
                    Key = key,
                    Value = message
                },
                cancellationToken);
        }

        private IProducer<string, CreateReviewEvent>? GetOrCreateProducer()
        {
            if (!_configurationValid)
            {
                return null;
            }

            if (_producer is not null)
            {
                return _producer;
            }

            lock (_sync)
            {
                if (_producer is not null)
                {
                    return _producer;
                }

                try
                {
                    var config = new ProducerConfig
                    {
                        BootstrapServers = _settings.BootstrapServers,
                        BrokerAddressFamily = BrokerAddressFamily.V4
                    };

                    _producer = new ProducerBuilder<string, CreateReviewEvent>(config)
                        .SetValueSerializer(new KafkaJsonSerializer<CreateReviewEvent>())
                        .Build();

                    const string eventTypeName = nameof(CreateReviewEvent);
                    if (!_settings.Topics.TryGetValue(eventTypeName, out var topic)
                        || string.IsNullOrWhiteSpace(topic))
                    {
                        _logger.LogWarning(
                            "Kafka topic is not configured for {EventType}",
                            eventTypeName);
                        _configurationValid = false;
                        return null;
                    }

                    _topic = topic;
                    return _producer;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize CreateReviewEvent Kafka producer");
                    _configurationValid = false;
                    return null;
                }
            }
        }

        private static bool IsConfigurationValid(KafkaProducerSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.BootstrapServers)
                && settings.Topics is not null
                && settings.Topics.TryGetValue(nameof(CreateReviewEvent), out var topic)
                && !string.IsNullOrWhiteSpace(topic);
        }
    }
}
