using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Filters;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Infrastructure.Quartz;
using LooksRatingApi.Messages.Kafka.PhotoRated;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers;
using LooksRatingApi.Services;
using LooksRatingApi.Services.BackGroundServices;
using LooksRatingApi.Services.BackGroundServices.Handlers;
using LooksRatingApi.Services.BackGroundServices.Jobs;
using LooksRatingApi.Services.PhotoServices;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Services.TheBestWeek;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace LooksRatingApi
{
    public static class Extensions
    {
        public static void AddDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<LooksRatingDbContext>(service =>
            {
                service.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            });
        }

        public static void AddBackGroundService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KafkaProducerSettings>(configuration.GetSection("Kafka"));
            services.Configure<KafkaConsumerSettings>(configuration.GetSection("KafkaConsumer:PhotoConsume"));

            services.AddHostedService<KafkaEnsureTopicsHostedService>();

            services.AddSingleton<IKafkaPhotoRatedProducer<PhotoRatedEvent>, KafkaPhotoRatedProducer<PhotoRatedEvent>>();
            services.AddScoped<IKafkaPhotoRatedConsumer<PhotoRatedEvent>, KafkaPhotoRatedConsumer<PhotoRatedEvent>>();

            services.AddScoped<IPhotoRecommendationService, PhotoRecommendationService>();
            services.AddSingleton<IFeedCycleStore, FeedCycleRedisStore>();
            services.AddScoped<IUnviewablePhotosProfilesService, UnviewablePhotosProfilesService>();
            services.AddScoped<IAddPhotoUsersCacheHandler, AddPhotoUsersCacheHandler>();

            services.AddHostedService<PhotoRatedBackgroundService<PhotoRatedEvent>>();
            services.AddHostedService<AddPhotoUsersCacheBackround>();
        }

        public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, PageParams? pageParams)
        {
            pageParams ??= new PageParams();
            var count = await query.CountAsync();
            if (count == 0) return new PagedResult<T>([], 0);
            var page = pageParams.Page ?? 1;
            var PageSize = pageParams.PageSize ?? 10;
            var skip = (page - 1) * PageSize;
            var result = await query.Skip(skip).Take(PageSize).ToListAsync();
            return new PagedResult<T>(result, count); 
        }

        public static void AddQuartz(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<LooksRatingQuartzOptions>(configuration.GetSection("Quartz"));
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LooksRatingQuartzOptions>>().Value;
                var timeZone = ApplicationTimeZoneResolver.Resolve(options.TimeZoneId);
                return new ApplicationClock(timeZone);
            });
            services.AddSingleton<IRedisDistributedLock, RedisDistributedLock>();
            services.AddSingleton<QuartzSchemaBootstrap>();
            services.AddSingleton<ArchivingLockService>();
            services.AddSingleton<TheBestWeekLockService>();
            services.AddScoped<INewListSeasonProcessor, NewListSeasonProcessor>();
            services.AddScoped<INewSeasonProcessor, NewSeasonProcessor>();
            services.AddScoped<ITheBestWeekProcessor, TheBestWeekProcessor>();
            services.AddScoped<IVipStatusExpiryProcessor, VipStatusExpiryProcessor>();
            services.AddScoped<IVipTopSparksRewardProcessor, VipTopSparksRewardProcessor>();

            services.AddQuartz((q, sp) =>
            {
                var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
                var quartzOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LooksRatingQuartzOptions>>().Value;
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string DefaultConnection is required.");

                var useClustering = quartzOptions.UseClustering || hostEnvironment.IsProduction();
                var scheduleTimeZone = ApplicationTimeZoneResolver.Resolve(quartzOptions.TimeZoneId);

                q.UseSimpleTypeLoader();
                q.UseDefaultThreadPool(tp => tp.MaxConcurrency = quartzOptions.MaxConcurrency);
                q.SchedulerName = quartzOptions.SchedulerName;
                q.SchedulerId = ResolveSchedulerInstanceId(quartzOptions);

                if (useClustering)
                {
                    q.UsePersistentStore(store =>
                    {
                        store.UseProperties = true;
                        store.RetryInterval = TimeSpan.FromSeconds(15);
                        store.UsePostgres(pg => pg.ConnectionString = connectionString);
                        store.UseNewtonsoftJsonSerializer();
                        store.UseClustering(c =>
                        {
                            c.CheckinInterval = TimeSpan.FromSeconds(20);
                            c.CheckinMisfireThreshold = TimeSpan.FromSeconds(40);
                        });
                    });
                }
                else
                {
                    q.UseInMemoryStore();
                }

                q.AddJob<NewListSeasonAddJob>(opts => opts
                    .WithIdentity(NewListSeasonAddJob.JobName)
                    .StoreDurably());

                q.AddJob<NewSeasonAddJob>(opts => opts
                    .WithIdentity(NewSeasonAddJob.JobName)
                    .StoreDurably());

                q.AddJob<TheBestWeekRefreshJob>(opts => opts
                    .WithIdentity(TheBestWeekRefreshJob.JobName)
                    .StoreDurably());

                q.AddJob<VipStatusExpiryJob>(opts => opts
                    .WithIdentity(VipStatusExpiryJob.JobName)
                    .StoreDurably());

                q.AddJob<VipTopSparksRewardJob>(opts => opts
                    .WithIdentity(VipTopSparksRewardJob.JobName)
                    .StoreDurably());

                q.AddTrigger(opts => opts
                    .ForJob(NewListSeasonAddJob.JobName)
                    .WithIdentity($"{NewListSeasonAddJob.JobName}-trigger")
                    .WithCronSchedule(quartzOptions.NewListSeasonCron, b => b.InTimeZone(scheduleTimeZone)));

                q.AddTrigger(opts => opts
                    .ForJob(NewSeasonAddJob.JobName)
                    .WithIdentity($"{NewSeasonAddJob.JobName}-trigger")
                    .WithCronSchedule(quartzOptions.NewSeasonCron, b => b.InTimeZone(scheduleTimeZone)));

                q.AddTrigger(opts => opts
                    .ForJob(TheBestWeekRefreshJob.JobName)
                    .WithIdentity($"{TheBestWeekRefreshJob.JobName}-trigger")
                    .WithCronSchedule(quartzOptions.TheBestWeekCron, b => b.InTimeZone(scheduleTimeZone)));

                q.AddTrigger(opts => opts
                    .ForJob(VipStatusExpiryJob.JobName)
                    .WithIdentity($"{VipStatusExpiryJob.JobName}-trigger")
                    .WithCronSchedule(quartzOptions.VipStatusExpiryCron, b => b.InTimeZone(scheduleTimeZone)));

                var vipRewardFirstFire = VipTopRewardSchedule.GetFirstFireTime(
                    scheduleTimeZone,
                    quartzOptions.VipTopSparksRewardHour,
                    quartzOptions.VipTopSparksRewardMinute);

                q.AddTrigger(opts => opts
                    .ForJob(VipTopSparksRewardJob.JobName)
                    .WithIdentity($"{VipTopSparksRewardJob.JobName}-trigger")
                    .StartAt(vipRewardFirstFire)
                    .WithCalendarIntervalSchedule(builder => builder
                        .WithIntervalInDays(VipTopRules.RewardPeriodDays)
                        .InTimeZone(scheduleTimeZone)));
            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            services.AddHostedService<QuartzSchedulerStartupLogger>();
        }

        private static string ResolveSchedulerInstanceId(LooksRatingQuartzOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.InstanceId))
                return options.InstanceId;

            var hostName = Environment.GetEnvironmentVariable("HOSTNAME");
            if (!string.IsNullOrWhiteSpace(hostName))
                return $"{hostName}-{Environment.ProcessId}";

            return $"{Environment.MachineName}-{Environment.ProcessId}";
        }
    }
}
