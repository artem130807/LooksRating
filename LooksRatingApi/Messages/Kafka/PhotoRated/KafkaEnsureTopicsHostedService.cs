using Confluent.Kafka;
using Confluent.Kafka.Admin;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Messages.Kafka.PhotoRated;

public sealed class KafkaEnsureTopicsHostedService : IHostedService
{
    private readonly KafkaProducerSettings _settings;
    private readonly ILogger<KafkaEnsureTopicsHostedService> _logger;

    public KafkaEnsureTopicsHostedService(
        IOptions<KafkaProducerSettings> options,
        ILogger<KafkaEnsureTopicsHostedService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.BootstrapServers) || _settings.Topics.Count == 0)
            return;

        var topicNames = _settings.Topics.Values.Distinct(StringComparer.Ordinal).ToList();
        var specs = topicNames.Select(name => new TopicSpecification
        {
            Name = name,
            NumPartitions = 1,
            ReplicationFactor = 1
        }).ToList();

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            BrokerAddressFamily = BrokerAddressFamily.V4
        }).Build();

        try
        {
            await admin.CreateTopicsAsync(specs, new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(30),
                OperationTimeout = TimeSpan.FromSeconds(30)
            }).ConfigureAwait(false);
        }
        catch (CreateTopicsException ex)
        {
            foreach (var r in ex.Results)
            {
                if (!r.Error.IsError)
                    continue;
                if (r.Error.Code == ErrorCode.TopicAlreadyExists)
                    continue;
                _logger.LogWarning("Kafka topic {Topic}: {Reason}", r.Topic, r.Error.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka: не удалось создать топики; при необходимости создайте вручную");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
