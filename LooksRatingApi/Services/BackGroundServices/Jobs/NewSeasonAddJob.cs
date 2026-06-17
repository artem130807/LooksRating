using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Infrastructure.Quartz;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class NewSeasonAddJob : IJob
    {
        public const string JobName = nameof(NewSeasonAddJob);

        private readonly INewSeasonProcessor _processor;
        private readonly ILogger<NewSeasonAddJob> _logger;

        public NewSeasonAddJob(
            INewSeasonProcessor processor,
            ILogger<NewSeasonAddJob> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context) =>
            QuartzJobExecutionLogger.ExecuteAsync(
                context,
                _logger,
                ct => _processor.ProcessMonthlyRolloverAsync(ct));
    }
}
