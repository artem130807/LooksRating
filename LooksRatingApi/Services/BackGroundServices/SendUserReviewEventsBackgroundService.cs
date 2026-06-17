using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.Consumer;

namespace LooksRatingApi.Services.BackGroundServices
{
    public sealed class SendUserReviewEventsBackgroundService : BackgroundService
    {
        private readonly ISendUserReviewConsumer<CreateReviewEvent> _consumer;
        private readonly ILogger<SendUserReviewEventsBackgroundService> _logger;

        public SendUserReviewEventsBackgroundService(
            ISendUserReviewConsumer<CreateReviewEvent> consumer,
            ILogger<SendUserReviewEventsBackgroundService> logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _consumer.ReadEvents(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SendUserReview Kafka consumer loop failed");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
