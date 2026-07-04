using LooksRatingApi.Contracts.ReviewContracts;

namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed class CreateReviewOutboxDispatcherBackgroundService : BackgroundService
    {
        private static readonly TimeSpan DispatchInterval = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CreateReviewOutboxDispatcherBackgroundService> _logger;

        public CreateReviewOutboxDispatcherBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<CreateReviewOutboxDispatcherBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var reviewBackgroundService = scope.ServiceProvider.GetRequiredService<IReviewBackgroundService>();
                    await reviewBackgroundService.EnqueuePendingOutboxAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispatch pending create-review outbox items");
                }

                try
                {
                    await Task.Delay(DispatchInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
