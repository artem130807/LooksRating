using LooksRatingApi.Contracts.SeasonLifecycle;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class NewListSeasonAddJob : IJob
    {
        public const string JobName = nameof(NewListSeasonAddJob);

        private readonly INewListSeasonProcessor _listSeasonProcessor;
        private readonly INewSeasonProcessor _seasonProcessor;

        public NewListSeasonAddJob(
            INewListSeasonProcessor listSeasonProcessor,
            INewSeasonProcessor seasonProcessor)
        {
            _listSeasonProcessor = listSeasonProcessor;
            _seasonProcessor = seasonProcessor;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var created = await _listSeasonProcessor.TryCreateNewChapterAsync(context.CancellationToken);
            if (!created)
                return;

            await _seasonProcessor.ProcessMonthlyRolloverAsync(context.CancellationToken);
        }
    }
}
