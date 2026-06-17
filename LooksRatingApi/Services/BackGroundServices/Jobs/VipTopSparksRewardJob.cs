using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Quartz;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class VipTopSparksRewardJob : IJob
    {
        public const string JobName = nameof(VipTopSparksRewardJob);

        private readonly IVipTopSparksRewardProcessor _processor;
        private readonly ILogger<VipTopSparksRewardJob> _logger;

        public VipTopSparksRewardJob(
            IVipTopSparksRewardProcessor processor,
            ILogger<VipTopSparksRewardJob> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context) =>
            QuartzJobExecutionLogger.ExecuteAsync(
                context,
                _logger,
                ct => _processor.ProcessAsync(ct));
    }
}
