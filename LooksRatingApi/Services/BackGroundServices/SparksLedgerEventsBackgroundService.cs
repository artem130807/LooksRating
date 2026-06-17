using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers.EventConsumer;

namespace LooksRatingApi.Services.BackGroundServices
{
    public sealed class SparksLedgerEventsBackgroundService : BackgroundService
    {
        private readonly IKafkaEventConsumer<CurrencySparksEvent> _creditsConsumer;
        private readonly IKafkaEventConsumer<CurrencyDebitCompensatedEvent> _compensationConsumer;
        private readonly ILogger<SparksLedgerEventsBackgroundService> _logger;

        public SparksLedgerEventsBackgroundService(
            IKafkaEventConsumer<CurrencySparksEvent> creditsConsumer,
            IKafkaEventConsumer<CurrencyDebitCompensatedEvent> compensationConsumer,
            ILogger<SparksLedgerEventsBackgroundService> logger)
        {
            _creditsConsumer = creditsConsumer;
            _compensationConsumer = compensationConsumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _creditsConsumer.ReadEvents(stoppingToken);
                    await _compensationConsumer.ReadEvents(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sparks ledger Kafka consumer loop failed");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
