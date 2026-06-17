using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.Consumer;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.Producers;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.Processing;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.BackGroundServices;
using LooksRatingApi.Services.ReviewMilestones;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.SendUserReview
{
    public static class SendUserReviewServiceCollectionExtensions
    {
        public static IServiceCollection AddSendUserReviewServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ReviewMilestoneNotificationOptions>(
                configuration.GetSection("ReviewMilestoneNotifications"));
            services.Configure<KafkaConsumerSettings>(
                "SendUserReview",
                configuration.GetSection("KafkaConsumer:SendUserReview"));

            services.AddSingleton<IReviewSequenceStore, RedisReviewSequenceStore>();
            services.AddSingleton<IReviewSequenceCalculator, ReviewSequenceCalculator>();
            services.AddSingleton<IReviewSequenceService, ReviewSequenceService>();
            services.AddSingleton<IReviewSequenceBootstrapper>(sp =>
            {
                var settings = sp.GetRequiredService<IOptionsMonitor<KafkaConsumerSettings>>()
                    .Get("SendUserReview");

                return new KafkaReviewSequenceBootstrapper(
                    Options.Create(settings),
                    sp.GetRequiredService<IReviewSequenceStore>(),
                    sp.GetRequiredService<ILogger<KafkaReviewSequenceBootstrapper>>());
            });
            services.AddSingleton<ICreateReviewEventProducer, KafkaCreateReviewEventProducer>();
            services.AddScoped<ICreateReviewEventPublisher, CreateReviewEventPublisher>();
            services.AddScoped<ISendUserReviewEventProcessor, SendUserReviewEventProcessor>();
            services.AddScoped<IReviewMilestoneNotifier, ReviewMilestoneNotifier>();
            services.AddScoped<IReviewMilestoneNotificationRepository, ReviewMilestoneNotificationRepository>();

            services.AddSingleton<ISendUserReviewConsumer<CreateReviewEvent>>(sp =>
            {
                var settings = sp.GetRequiredService<IOptionsMonitor<KafkaConsumerSettings>>()
                    .Get("SendUserReview");

                return new SendUserReviewConsumer<CreateReviewEvent>(
                    Options.Create(settings),
                    sp.GetRequiredService<ILogger<SendUserReviewConsumer<CreateReviewEvent>>>(),
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<IReviewSequenceBootstrapper>());
            });

            services.AddHostedService<SendUserReviewEventsBackgroundService>();

            return services;
        }
    }
}
