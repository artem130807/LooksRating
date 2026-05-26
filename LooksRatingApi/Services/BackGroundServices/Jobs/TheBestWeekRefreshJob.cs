using LooksRatingApi.Contracts.TheBestWeekContracts;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class TheBestWeekRefreshJob : IJob
    {
        public const string JobName = nameof(TheBestWeekRefreshJob);

        private readonly ITheBestWeekProcessor _processor;

        public TheBestWeekRefreshJob(ITheBestWeekProcessor processor)
        {
            _processor = processor;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _processor.RefreshWeeklyAsync(context.CancellationToken);
        }
    }
}
