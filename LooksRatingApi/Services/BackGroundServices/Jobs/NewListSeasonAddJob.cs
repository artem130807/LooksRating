using LooksRatingApi.Contracts.SeasonLifecycle;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class NewListSeasonAddJob : IJob
    {
        public const string JobName = nameof(NewListSeasonAddJob);

        private readonly INewListSeasonProcessor _processor;

        public NewListSeasonAddJob(INewListSeasonProcessor processor)
        {
            _processor = processor;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _processor.TryCreateNewChapterAsync(context.CancellationToken);

            await context.Scheduler.TriggerJob(
                new JobKey(NewSeasonAddJob.JobName),
                context.CancellationToken);
        }
    }
}
