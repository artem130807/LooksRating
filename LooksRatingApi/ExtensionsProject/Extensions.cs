using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Filters;
using LooksRatingApi.Messages.Kafka.PhotoRated;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers;
using LooksRatingApi.Services;
using LooksRatingApi.Services.BackGroundServices;
using LooksRatingApi.Services.BackGroundServices.Handlers;
using LooksRatingApi.Services.BackGroundServices.Jobs;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Services.TheBestWeek;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<ArchivingLockService>();
            services.AddSingleton<TheBestWeekLockService>();
            services.AddScoped<INewListSeasonProcessor, NewListSeasonProcessor>();
            services.AddScoped<INewSeasonProcessor, NewSeasonProcessor>();
            services.AddScoped<ITheBestWeekProcessor, TheBestWeekProcessor>();

            var newListSeasonCron = configuration["Quartz:NewListSeasonCron"] ?? "0 0 0 1 1 ?";
            var newSeasonCron = configuration["Quartz:NewSeasonCron"] ?? "0 0 0 1 2-12 ?";
            var theBestWeekCron = configuration["Quartz:TheBestWeekCron"] ?? "0 0 0 ? * MON";

            services.AddQuartz(q =>
            {
                q.UseSimpleTypeLoader();
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 10);

                q.AddJob<NewListSeasonAddJob>(opts => opts
                    .WithIdentity(NewListSeasonAddJob.JobName));

                q.AddJob<NewSeasonAddJob>(opts => opts
                    .WithIdentity(NewSeasonAddJob.JobName));

                q.AddJob<TheBestWeekRefreshJob>(opts => opts
                    .WithIdentity(TheBestWeekRefreshJob.JobName));

                q.AddTrigger(opts => opts
                    .ForJob(NewListSeasonAddJob.JobName)
                    .WithIdentity($"{NewListSeasonAddJob.JobName}-trigger")
                    .WithCronSchedule(newListSeasonCron));

                q.AddTrigger(opts => opts
                    .ForJob(NewSeasonAddJob.JobName)
                    .WithIdentity($"{NewSeasonAddJob.JobName}-trigger")
                    .WithCronSchedule(newSeasonCron));

                q.AddTrigger(opts => opts
                    .ForJob(TheBestWeekRefreshJob.JobName)
                    .WithIdentity($"{TheBestWeekRefreshJob.JobName}-trigger")
                    .WithCronSchedule(theBestWeekCron));
            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });
        }
    }
}