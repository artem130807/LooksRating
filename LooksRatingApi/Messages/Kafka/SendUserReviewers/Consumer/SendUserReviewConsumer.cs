using Confluent.Kafka;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.Processing;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.Consumer
{
    public sealed class SendUserReviewConsumer<TMessage> : ISendUserReviewConsumer<TMessage>
        where TMessage : DomainEvent
    {
        private readonly KafkaConsumerSettings _settings;
        private readonly ILogger<SendUserReviewConsumer<TMessage>> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IReviewSequenceBootstrapper _sequenceBootstrapper;
        private readonly object _consumerLock = new();
        private readonly SemaphoreSlim _bootstrapLock = new(1, 1);
        private IConsumer<string, TMessage>? _consumer;
        private bool _disposed;
        private bool _configurationValid;
        private bool _sequenceBootstrapped;

        public SendUserReviewConsumer(
            IOptions<KafkaConsumerSettings> options,
            ILogger<SendUserReviewConsumer<TMessage>> logger,
            IServiceScopeFactory scopeFactory,
            IReviewSequenceBootstrapper sequenceBootstrapper)
        {
            _settings = options.Value ?? new KafkaConsumerSettings();
            _logger = logger;
            _scopeFactory = scopeFactory;
            _sequenceBootstrapper = sequenceBootstrapper;
            _configurationValid = IsConfigurationValid(_settings);

            if (!_configurationValid)
            {
                _logger.LogWarning(
                    "SendUserReview Kafka consumer for {EventType} is not configured",
                    typeof(TMessage).Name);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _bootstrapLock.Dispose();

            lock (_consumerLock)
            {
                _consumer?.Close();
                _consumer?.Dispose();
                _consumer = null;
            }
        }

        public Task ProcessEvent(TMessage message, CancellationToken cancellationToken)
        {
            return DispatchAsync(message, cancellationToken);
        }

        public async Task ReadEvents(CancellationToken cancellationToken)
        {
            if (!_configurationValid)
            {
                return;
            }

            await EnsureSequenceBootstrappedAsync(cancellationToken);

            var consumer = GetOrCreateConsumer();
            if (consumer is null)
            {
                return;
            }

            var timeout = TimeSpan.FromSeconds(5);
            try
            {
                var consumeResult = consumer.Consume(timeout);
                if (consumeResult?.Message?.Value is null)
                {
                    return;
                }

                await ProcessEvent(consumeResult.Message.Value, cancellationToken);
                consumer.Commit(consumeResult);
            }
            catch (ConsumeException ex)
            {
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning(
                        "Topic {Topic} is not available yet for {EventType}: {Reason}",
                        _settings.Topic,
                        typeof(TMessage).Name,
                        ex.Error.Reason);
                    return;
                }

                _logger.LogError(ex, "Error reading CreateReview events from topic {Topic}", _settings.Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected SendUserReview Kafka consumer error for topic {Topic}", _settings.Topic);
            }
        }

        private async Task EnsureSequenceBootstrappedAsync(CancellationToken cancellationToken)
        {
            if (_sequenceBootstrapped)
            {
                return;
            }

            await _bootstrapLock.WaitAsync(cancellationToken);
            try
            {
                if (_sequenceBootstrapped)
                {
                    return;
                }

                await _sequenceBootstrapper.BootstrapAsync(cancellationToken);
                _sequenceBootstrapped = true;
            }
            finally
            {
                _bootstrapLock.Release();
            }
        }

        private static bool IsConfigurationValid(KafkaConsumerSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.BootstrapServers)
                && !string.IsNullOrWhiteSpace(settings.Topic)
                && !string.IsNullOrWhiteSpace(settings.GroupId);
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
                        AllowAutoCreateTopics = true,
                        AutoOffsetReset = AutoOffsetReset.Earliest,
                        EnableAutoCommit = false
                    };

                    _consumer = new ConsumerBuilder<string, TMessage>(config)
                        .SetValueDeserializer(new KafkaValueDeserializer<TMessage>())
                        .Build();
                    _consumer.Subscribe(_settings.Topic);
                    _logger.LogInformation(
                        "Subscribed to topic {Topic} for {EventType}",
                        _settings.Topic,
                        typeof(TMessage).Name);
                    return _consumer;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to initialize SendUserReview Kafka consumer for {EventType}",
                        typeof(TMessage).Name);
                    _configurationValid = false;
                    return null;
                }
            }
        }

        private async Task DispatchAsync(TMessage message, CancellationToken cancellationToken)
        {
            if (message is not CreateReviewEvent createReviewEvent)
            {
                _logger.LogWarning(
                    "Skipped unsupported event type {EventType} in SendUserReview consumer",
                    message.GetType().Name);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ISendUserReviewEventProcessor>();
            await processor.ProcessAsync(createReviewEvent, cancellationToken);
        }
    }
}
