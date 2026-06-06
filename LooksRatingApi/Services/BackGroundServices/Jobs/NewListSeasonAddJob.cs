using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Infrastructure.Quartz;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class NewListSeasonAddJob : IJob
    {
        public const string JobName = nameof(NewListSeasonAddJob);

        private readonly INewListSeasonProcessor _listSeasonProcessor;
        private readonly INewSeasonProcessor _seasonProcessor;
        private readonly ILogger<NewListSeasonAddJob> _logger;

        public NewListSeasonAddJob(
            INewListSeasonProcessor listSeasonProcessor,
            INewSeasonProcessor seasonProcessor,
            ILogger<NewListSeasonAddJob> logger)
        {
            _listSeasonProcessor = listSeasonProcessor;
            _seasonProcessor = seasonProcessor;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context) =>
            QuartzJobExecutionLogger.ExecuteAsync(context, _logger, async ct =>
            {
                var created = await _listSeasonProcessor.TryCreateNewChapterAsync(ct);
                if (!created)
                {
                    _logger.LogInformation("Quartz [{JobName}]: новая глава не создана — смена сезона пропущена", JobName);
                    return;
                }

                _logger.LogInformation("Quartz [{JobName}]: глава создана, запуск смены сезона", JobName);
                await _seasonProcessor.ProcessMonthlyRolloverAsync(ct);
            });
    }
}
