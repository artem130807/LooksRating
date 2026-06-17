using LooksRatingApi.Domain.Base;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer
{
    public sealed class LazyKafkaEventProducer<TMessage> : IKafkaEventProducer<TMessage> where TMessage : DomainEvent
    {
        private readonly IOptions<KafkaEventProducerSettings> _options;
        private readonly ILogger<LazyKafkaEventProducer<TMessage>> _logger;
        private readonly object _sync = new();
        private KafkaEventProducer<TMessage>? _inner;

        public LazyKafkaEventProducer(
            IOptions<KafkaEventProducerSettings> options,
            ILogger<LazyKafkaEventProducer<TMessage>> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _inner?.Dispose();
                _inner = null;
            }
        }

        public Task Produce(TMessage message, CancellationToken cancellationToken)
        {
            var producer = GetOrCreateProducer();
            if (producer is null)
            {
                _logger.LogWarning(
                    "Kafka producer for {EventType} is not configured; event skipped",
                    typeof(TMessage).Name);
                return Task.CompletedTask;
            }

            return producer.Produce(message, cancellationToken);
        }

        private KafkaEventProducer<TMessage>? GetOrCreateProducer()
        {
            if (_inner is not null)
            {
                return _inner;
            }

            lock (_sync)
            {
                if (_inner is not null)
                {
                    return _inner;
                }

                var settings = _options.Value;
                var typeName = typeof(TMessage).Name;
                if (string.IsNullOrWhiteSpace(settings.BootstrapServers)
                    || settings.Topics is null
                    || !settings.Topics.TryGetValue(typeName, out var topic)
                    || string.IsNullOrWhiteSpace(topic))
                {
                    _logger.LogWarning(
                        "Kafka producer settings are incomplete for {EventType}",
                        typeName);
                    return null;
                }

                _inner = new KafkaEventProducer<TMessage>(_options);
                return _inner;
            }
        }
    }
}
