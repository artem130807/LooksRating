using System.Diagnostics;
using Quartz;

namespace LooksRatingApi.Infrastructure.Quartz
{
    internal static class QuartzJobExecutionLogger
    {
        public static async Task ExecuteAsync<TJob>(
            IJobExecutionContext context,
            ILogger<TJob> logger,
            Func<CancellationToken, Task> action)
        {
            var jobName = context.JobDetail.Key.Name;
            var triggerName = context.Trigger.Key.Name;
            var fireTime = context.FireTimeUtc;
            var schedulerId = context.Scheduler.SchedulerInstanceId;
            var sw = Stopwatch.StartNew();

            logger.LogInformation(
                "Quartz [{JobName}] старт: fire={FireTime:u}, trigger={Trigger}, scheduler={SchedulerId}",
                jobName,
                fireTime,
                triggerName,
                schedulerId);

            try
            {
                await action(context.CancellationToken);
                sw.Stop();
                logger.LogInformation(
                    "Quartz [{JobName}] завершён за {ElapsedMs} мс",
                    jobName,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError(
                    ex,
                    "Quartz [{JobName}] ошибка после {ElapsedMs} мс",
                    jobName,
                    sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
