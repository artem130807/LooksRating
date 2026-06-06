using LooksRatingApi.Contracts.PhotoUserContracts;

namespace LooksRatingApi.Services.BackGroundServices
{
    public class AddPhotoUsersCacheBackround : BackgroundService
    {
        private readonly ILogger<AddPhotoUsersCacheBackround> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public AddPhotoUsersCacheBackround(ILogger<AddPhotoUsersCacheBackround> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Фоновый кэш фото: сервис запущен, интервал {Minutes} мин",
                _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Фоновый кэш фото: цикл обновления Redis sorted set");
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IAddPhotoUsersCacheHandler>();
                    await handler.Handle(stoppingToken);
                    _logger.LogInformation("Фоновый кэш фото: цикл завершён, пауза {Minutes} мин", _interval.TotalMinutes);
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка фонового обновления кэша фотографий");
                }
            }
        }
    }
}
