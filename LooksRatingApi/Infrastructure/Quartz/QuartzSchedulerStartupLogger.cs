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
        private readonly IHostEnvironment _environment;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<QuartzSchedulerStartupLogger> _logger;

        public QuartzSchedulerStartupLogger(
            ISchedulerFactory schedulerFactory,
            IOptions<LooksRatingQuartzOptions> options,
            ApplicationClock clock,
            IHostEnvironment environment,
            IHostApplicationLifetime lifetime,
            ILogger<QuartzSchedulerStartupLogger> logger)
        {
            _schedulerFactory = schedulerFactory;
            _options = options.Value;
            _clock = clock;
            _environment = environment;
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
                "QuartzStartup: cron VipStatusExpiry={VipCron}, TheBestWeek={WeekCron}, NewSeason={SeasonCron}, NewListSeason={ChapterCron}",
                _options.VipStatusExpiryCron,
                _options.TheBestWeekCron,
                _options.NewSeasonCron,
                _options.NewListSeasonCron);

            if (_options.SkipCalendarGuards || _options.SkipListSeasonNumberGuard)
            {
                _logger.LogWarning(
                    "QuartzStartup: test flags SkipCalendarGuards={SkipCalendar}, SkipListSeasonNumberGuard={SkipChapter}",
                    _options.SkipCalendarGuards,
                    _options.SkipListSeasonNumberGuard);
            }

            if (_options.MirrorProductionCron)
            {
                _logger.LogWarning(
                    "QuartzStartup: MirrorProductionCron enabled (prod MSK schedule), SkipCalendarGuards={Skip}",
                    _options.SkipCalendarGuards);
            }

            if (_environment.IsEnvironment("TestQuartz"))
                await SyncTestQuartzTriggersAsync(scheduler);

            await LogNextFireTimesAsync(scheduler);
        }

        private static async Task WaitForSchedulerStartedAsync(IScheduler scheduler)
        {
            for (var attempt = 0; attempt < 120 && !scheduler.IsStarted; attempt++)
                await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        private async Task SyncTestQuartzTriggersAsync(IScheduler scheduler)
        {
            var scheduleTimeZone = ApplicationTimeZoneResolver.Resolve(_options.TimeZoneId);
            var triggerDefs = new (string JobName, string Cron)[]
            {
                (VipStatusExpiryJob.JobName, _options.VipStatusExpiryCron),
                (TheBestWeekRefreshJob.JobName, _options.TheBestWeekCron),
                (NewSeasonAddJob.JobName, _options.NewSeasonCron),
                (NewListSeasonAddJob.JobName, _options.NewListSeasonCron),
            };

            foreach (var (jobName, cron) in triggerDefs)
            {
                var triggerKey = new TriggerKey($"{jobName}-trigger");
                var newTrigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .ForJob(jobName)
                    .WithCronSchedule(cron, b => b.InTimeZone(scheduleTimeZone))
                    .Build();

                if (!await scheduler.CheckExists(triggerKey))
                    continue;

                await scheduler.RescheduleJob(triggerKey, newTrigger);
                _logger.LogInformation(
                    "QuartzStartup: trigger {Trigger} rescheduled from appsettings (cron={Cron})",
                    triggerKey.Name,
                    cron);
            }
        }

        private async Task LogNextFireTimesAsync(IScheduler scheduler)
        {
            var scheduleTimeZone = ApplicationTimeZoneResolver.Resolve(_options.TimeZoneId);
            var jobNames = new[]
            {
                VipStatusExpiryJob.JobName,
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
