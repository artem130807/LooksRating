using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Infrastructure.Quartz;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class TheBestWeekRefreshJob : IJob
    {
        public const string JobName = nameof(TheBestWeekRefreshJob);

        private readonly ITheBestWeekProcessor _processor;
        private readonly ILogger<TheBestWeekRefreshJob> _logger;

        public TheBestWeekRefreshJob(
            ITheBestWeekProcessor processor,
            ILogger<TheBestWeekRefreshJob> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context) =>
            QuartzJobExecutionLogger.ExecuteAsync(
                context,
                _logger,
                ct => _processor.RefreshWeeklyAsync(ct));
    }
}
