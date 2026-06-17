using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers;
using LooksRatingApi.Messages.Kafka.PhotoRated.Consumers.EventConsumer;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.BackGroundServices;
using LooksRatingApi.Services.Orchestrators;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Services.SparksWallet;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.SparksWallet
{
    public static class SparksWalletServiceCollectionExtensions
    {
        public static IServiceCollection AddSparksWalletServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<KafkaEventProducerSettings>(configuration.GetSection("Kafka"));
            services.Configure<KafkaConsumerSettings>(
                "SparksCredits",
                configuration.GetSection("KafkaConsumer:SparksCredits"));
            services.Configure<KafkaConsumerSettings>(
                "SparksCompensation",
                configuration.GetSection("KafkaConsumer:SparksCompensation"));

            services.AddScoped<IEventStoreRepository, EventStoreRepository>();
            services.AddScoped<ICurrencyDebitedService, CurrencyDebitedService>();
            services.AddScoped<ICurrencySparksService, CurrencySparksService>();
            services.AddScoped<ISparksWalletProvisioner, SparksWalletProvisioner>();
            services.AddScoped<IReviewSparksRewardService, ReviewSparksRewardService>();
            services.AddScoped<IRatedProfileSparksRewardService, RatedProfileSparksRewardService>();
            services.AddScoped<ICurrencyCreditedSparksByLinkService, CurrencyCreditedSparksByLinkService>();
            services.AddScoped<ICurrencyDebitCompensatedService, CurrencyDebitCompensatedService>();
            services.AddScoped<IChangeSparksLedgersService, ChangeSparksLedgersService>();
            services.AddScoped<ISparksLedgerEventDispatcher, SparksLedgerEventDispatcher>();
            services.AddScoped<IDebitedSparksOrchestrator, DebitedSparksOrchestrator>();
            services.AddScoped<IRollBackDebitedSparksOrchestrator, RollBackDebitedSparksOrchestrator>();

            services.AddSingleton<IKafkaEventProducer<CurrencyDebitedEvent>, LazyKafkaEventProducer<CurrencyDebitedEvent>>();
            services.AddSingleton<IKafkaEventProducer<CurrencySparksEvent>, LazyKafkaEventProducer<CurrencySparksEvent>>();
            services.AddSingleton<IKafkaEventProducer<CurrencyDebitCompensatedEvent>, LazyKafkaEventProducer<CurrencyDebitCompensatedEvent>>();

            services.AddSingleton<IKafkaEventConsumer<CurrencySparksEvent>>(sp =>
                CreateSparksConsumer<CurrencySparksEvent>(sp, "SparksCredits"));
            services.AddSingleton<IKafkaEventConsumer<CurrencyDebitCompensatedEvent>>(sp =>
                CreateSparksConsumer<CurrencyDebitCompensatedEvent>(sp, "SparksCompensation"));

            services.AddHostedService<SparksLedgerEventsBackgroundService>();

            return services;
        }

        private static KafkaEventConsumer<TMessage> CreateSparksConsumer<TMessage>(
            IServiceProvider serviceProvider,
            string settingsName) where TMessage : DomainEvent
        {
            var settings = serviceProvider
                .GetRequiredService<IOptionsMonitor<KafkaConsumerSettings>>()
                .Get(settingsName);

            return new KafkaEventConsumer<TMessage>(
                Options.Create(settings),
                serviceProvider.GetRequiredService<ILogger<KafkaEventConsumer<TMessage>>>(),
                serviceProvider.GetRequiredService<IServiceScopeFactory>());
        }
    }
}
