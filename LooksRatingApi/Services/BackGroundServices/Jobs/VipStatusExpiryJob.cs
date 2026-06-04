using LooksRatingApi.Contracts;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class VipStatusExpiryJob : IJob
    {
        public const string JobName = nameof(VipStatusExpiryJob);

        private readonly IVipStatusExpiryProcessor _processor;

        public VipStatusExpiryJob(IVipStatusExpiryProcessor processor)
        {
            _processor = processor;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _processor.ProcessAsync(context.CancellationToken);
        }
    }
}
