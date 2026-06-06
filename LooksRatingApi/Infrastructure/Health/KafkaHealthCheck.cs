using Confluent.Kafka;
using LooksRatingApi.Infrastructure.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers;

namespace LooksRatingApi.Infrastructure.Health
{
    public sealed class KafkaHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly KafkaConsumerSettings _settings;

        public KafkaHealthCheck(
            IConfiguration configuration,
            IOptions<KafkaConsumerSettings> settings)
        {
            _configuration = configuration;
            _settings = settings.Value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (!KafkaFeature.IsEnabled(_configuration))
            {
                return Task.FromResult(HealthCheckResult.Healthy("Kafka disabled"));
            }

            if (string.IsNullOrWhiteSpace(_settings.BootstrapServers))
            {
                return Task.FromResult(HealthCheckResult.Healthy("Kafka bootstrap servers are not configured"));
            }

            try
            {
                using var admin = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _settings.BootstrapServers
                }).Build();

                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
                if (metadata.Brokers.Count == 0)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Kafka cluster has no brokers"));
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Kafka reachable ({metadata.Brokers.Count} broker(s))"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Kafka is unreachable", ex));
            }
        }
    }
}
