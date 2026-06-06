namespace LooksRatingApi.Infrastructure.Quartz
{
    public sealed class LooksRatingQuartzOptions
    {
        public string TimeZoneId { get; set; } = ApplicationTimeZoneResolver.DefaultTimeZoneId;

        public bool UseClustering { get; set; }

        public string SchedulerName { get; set; } = "LooksRatingScheduler";

        public string? InstanceId { get; set; }

        public int MaxConcurrency { get; set; } = 10;

        public bool AutoCreateSchema { get; set; } = true;

        public bool SkipCalendarGuards { get; set; }

        public bool SkipListSeasonNumberGuard { get; set; }

        /// <summary>
        /// TestQuartz: подставить cron из prod (MSK). Сезонные job всё равно требуют SkipCalendarGuards для ручного прогона.
        /// </summary>
        public bool MirrorProductionCron { get; set; }

        public string NewListSeasonCron { get; set; } = "0 0 0 1 1 ?";

        public string NewSeasonCron { get; set; } = "0 0 0 1 2-12 ?";

        public string TheBestWeekCron { get; set; } = "0 0 0 ? * MON";

        public string VipStatusExpiryCron { get; set; } = "0 0 * * * ?";
    }
}
