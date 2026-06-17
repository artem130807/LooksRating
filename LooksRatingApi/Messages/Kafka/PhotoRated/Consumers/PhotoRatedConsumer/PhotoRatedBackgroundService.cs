using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Consumers
{
    public class PhotoRatedBackgroundService<TMessage> : BackgroundService where TMessage: DomainEvent
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PhotoRatedBackgroundService<TMessage>> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
        public PhotoRatedBackgroundService(IServiceProvider serviceProvider, ILogger<PhotoRatedBackgroundService<TMessage>> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;  
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try 
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var handlerSalon = scope.ServiceProvider
                        .GetRequiredService<IKafkaPhotoRatedConsumer<TMessage>>();
                        await handlerSalon.ReadEvents(stoppingToken);
                    }
                    _logger.LogInformation($"Жду {_interval.TotalMinutes} минут до следующей обработки");
                    await Task.Delay(_interval, stoppingToken);   
                }catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                }       
            }
        }
    }
}