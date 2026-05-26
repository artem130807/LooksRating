using LooksRatingApi.Contracts.SeasonLifecycle;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class NewSeasonAddJob : IJob
    {
        public const string JobName = nameof(NewSeasonAddJob);

        private readonly INewSeasonProcessor _processor;

        public NewSeasonAddJob(INewSeasonProcessor processor)
        {
            _processor = processor;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _processor.ProcessMonthlyRolloverAsync(context.CancellationToken);
        }
    }
}
