using LooksRatingApi.Services;
using LooksRatingApi.Services.BackGroundServices.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;

namespace LooksRatingApi.Infrastructure.Quartz
{
    public sealed class QuartzSchedulerStartupLogger : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly LooksRatingQuartzOptions _options;
        private readonly ApplicationClock _clock;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<QuartzSchedulerStartupLogger> _logger;

        public QuartzSchedulerStartupLogger(
            ISchedulerFactory schedulerFactory,
            IOptions<LooksRatingQuartzOptions> options,
            ApplicationClock clock,
            IHostApplicationLifetime lifetime,
            ILogger<QuartzSchedulerStartupLogger> logger)
        {
            _schedulerFactory = schedulerFactory;
            _options = options.Value;
            _clock = clock;
            _lifetime = lifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lifetime.ApplicationStarted.Register(() =>
            {
                _ = LogSchedulerStateSafeAsync();
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task LogSchedulerStateSafeAsync()
        {
            try
            {
                await LogSchedulerStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QuartzStartup: не удалось вывести состояние планировщика");
            }
        }

        private async Task LogSchedulerStateAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await WaitForSchedulerStartedAsync(scheduler);

            var now = _clock.GetNow();

            _logger.LogInformation(
                "QuartzStartup: scheduler name={SchedulerName}, instance={InstanceId}, tz={TimeZone}, now={Now:O}, started={Started}, clustering={Clustering}",
                scheduler.SchedulerName,
                scheduler.SchedulerInstanceId,
                _options.TimeZoneId,
                now,
                scheduler.IsStarted,
                _options.UseClustering);

            _logger.LogInformation(
                "QuartzStartup: cron VipStatusExpiry={VipCron}, VipTopSparksReward=every {SparksIntervalDays}d at {SparksHour:D2}:{SparksMinute:D2}, TheBestWeek={WeekCron}, NewSeason={SeasonCron}, NewListSeason={ChapterCron}",
                _options.VipStatusExpiryCron,
                VipTopRules.RewardPeriodDays,
                _options.VipTopSparksRewardHour,
                _options.VipTopSparksRewardMinute,
                _options.TheBestWeekCron,
                _options.NewSeasonCron,
                _options.NewListSeasonCron);

            await LogNextFireTimesAsync(scheduler);
        }

        private static async Task WaitForSchedulerStartedAsync(IScheduler scheduler)
        {
            for (var attempt = 0; attempt < 120 && !scheduler.IsStarted; attempt++)
                await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        private async Task LogNextFireTimesAsync(IScheduler scheduler)
        {
            var scheduleTimeZone = ApplicationTimeZoneResolver.Resolve(_options.TimeZoneId);
            var jobNames = new[]
            {
                VipStatusExpiryJob.JobName,
                VipTopSparksRewardJob.JobName,
                TheBestWeekRefreshJob.JobName,
                NewSeasonAddJob.JobName,
                NewListSeasonAddJob.JobName,
            };

            foreach (var jobName in jobNames)
            {
                var triggers = await scheduler.GetTriggersOfJob(new JobKey(jobName));
                foreach (var trigger in triggers)
                {
                    var nextUtc = trigger.GetNextFireTimeUtc();
                    var nextLocal = nextUtc.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(nextUtc.Value.UtcDateTime, scheduleTimeZone)
                        : (DateTimeOffset?)null;

                    _logger.LogInformation(
                        "QuartzStartup: next fire job={JobName}, trigger={Trigger}, at={NextLocal:O} ({TimeZone})",
                        jobName,
                        trigger.Key.Name,
                        nextLocal,
                        _options.TimeZoneId);
                }
            }
        }
    }
}
