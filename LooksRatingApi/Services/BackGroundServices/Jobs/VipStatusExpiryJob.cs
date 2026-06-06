using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Quartz;
using Quartz;

namespace LooksRatingApi.Services.BackGroundServices.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class VipStatusExpiryJob : IJob
    {
        public const string JobName = nameof(VipStatusExpiryJob);

        private readonly IVipStatusExpiryProcessor _processor;
        private readonly ILogger<VipStatusExpiryJob> _logger;

        public VipStatusExpiryJob(
            IVipStatusExpiryProcessor processor,
            ILogger<VipStatusExpiryJob> logger)
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
