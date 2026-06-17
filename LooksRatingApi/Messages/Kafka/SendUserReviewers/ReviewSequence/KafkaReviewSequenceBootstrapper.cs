using Confluent.Kafka;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    /// <summary>
    /// Восстанавливает последний ReviewsCount по каждому PhotoProfileId
    /// из истории Kafka-топика перед обработкой новых событий.
    /// </summary>
    public sealed class KafkaReviewSequenceBootstrapper : IReviewSequenceBootstrapper
    {
        private readonly KafkaConsumerSettings _settings;
        private readonly IReviewSequenceStore _sequenceStore;
        private readonly ILogger<KafkaReviewSequenceBootstrapper> _logger;
        private readonly bool _configurationValid;

        public KafkaReviewSequenceBootstrapper(
            IOptions<KafkaConsumerSettings> options,
            IReviewSequenceStore sequenceStore,
            ILogger<KafkaReviewSequenceBootstrapper> logger)
        {
            _settings = options.Value ?? new KafkaConsumerSettings();
            _sequenceStore = sequenceStore;
            _logger = logger;
            _configurationValid = IsConfigurationValid(_settings);

            if (!_configurationValid)
            {
                _logger.LogWarning("Review sequence bootstrapper is not configured");
            }
        }

        public Task BootstrapAsync(CancellationToken cancellationToken)
        {
            if (!_configurationValid)
            {
                return Task.CompletedTask;
            }

            return Task.Run(() => BootstrapCore(cancellationToken), cancellationToken);
        }

        private void BootstrapCore(CancellationToken cancellationToken)
        {
            var latestByKey = new Dictionary<ReviewSequenceKey, int>();

            using var consumer = new ConsumerBuilder<string, CreateReviewEvent>(BuildConfig())
                .SetValueDeserializer(new KafkaValueDeserializer<CreateReviewEvent>())
                .Build();

            consumer.Subscribe(_settings.Topic);

            var idleRounds = 0;
            const int maxIdleRounds = 3;

            while (!cancellationToken.IsCancellationRequested && idleRounds < maxIdleRounds)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult is null)
                {
                    idleRounds++;
                    continue;
                }

                idleRounds = 0;

                if (consumeResult.IsPartitionEOF)
                {
                    continue;
                }

                var @event = consumeResult.Message.Value;
                if (@event is null || @event.ReviewsCount < 1)
                {
                    continue;
                }

                var key = ResolveSequenceKey(consumeResult.Message.Key, @event);
                latestByKey[key] = @event.ReviewsCount;
            }

            foreach (var (key, count) in latestByKey)
            {
                _sequenceStore.SetLastReviewsCount(key, count);
            }

            _logger.LogInformation(
                "Review sequence state bootstrapped from topic {Topic}: {ProfilesCount} profiles",
                _settings.Topic,
                latestByKey.Count);
        }

        private ConsumerConfig BuildConfig()
        {
            return new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = $"{_settings.GroupId}-sequence-bootstrap-{Guid.NewGuid():N}",
                BrokerAddressFamily = BrokerAddressFamily.V4,
                AllowAutoCreateTopics = true,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
        }

        private static ReviewSequenceKey ResolveSequenceKey(string? kafkaKey, CreateReviewEvent @event)
        {
            return ReviewSequenceKey.TryParseKafkaKey(kafkaKey, out var parsed)
                ? parsed
                : ReviewSequenceKey.From(@event);
        }

        private static bool IsConfigurationValid(KafkaConsumerSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.BootstrapServers)
                && !string.IsNullOrWhiteSpace(settings.Topic)
                && !string.IsNullOrWhiteSpace(settings.GroupId);
        }
    }
}
