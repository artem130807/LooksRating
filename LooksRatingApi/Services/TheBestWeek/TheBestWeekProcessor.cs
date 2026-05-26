using System.Text.Json;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace LooksRatingApi.Services.TheBestWeek
{
    public sealed class TheBestWeekProcessor : ITheBestWeekProcessor
    {
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(5);
        private readonly ITheBestWeekRepository _theBestWeekRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly TheBestWeekLockService _lockService;
        private readonly ArchivingLockService _archivingLockService;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ILogger<TheBestWeekProcessor> _logger;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly StackExchange.Redis.IDatabase _db;
        private readonly IConnectionMultiplexer _redis;

        public TheBestWeekProcessor(
            ITheBestWeekRepository theBestWeekRepository,
            IMemoryCache memoryCache,
            TheBestWeekLockService lockService,
            ArchivingLockService archivingLockService,
            ILogger<TheBestWeekProcessor> logger,
            ISeasonRepository seasonRepository,
            INormalizeCityNameService normalizeCityNameService,
            IConnectionMultiplexer redis,
            IPhotoUserRepository photoUserRepository)
        {
            _theBestWeekRepository = theBestWeekRepository;
            _memoryCache = memoryCache;
            _lockService = lockService;
            _archivingLockService = archivingLockService;
            _logger = logger;
            _seasonRepository = seasonRepository;
            _normalizeCityNameService = normalizeCityNameService;
            _redis = redis;
            _db = _redis.GetDatabase();
            _photoUserRepository = photoUserRepository;
        }

        public async Task RefreshWeeklyAsync(CancellationToken cancellationToken)
        {
            if (!_memoryCache.TryGetValue<HashSet<string>>("key_cities_names", out var cityNames) || cityNames is null)
            {
                _logger.LogWarning("Список городов не загружен, обновление лучшей недели пропущено");
                return;
            }

            if (await _archivingLockService.IsArchivingInProgressAsync())
            {
                _logger.LogWarning("Обновление лучшей недели пропущено: идёт архивация сезона");
                return;
            }

            if (await _lockService.IsRefreshInProgressAsync())
            {
                _logger.LogWarning("Обновление лучшей недели уже выполняется");
                return;
            }

            var period = WeekPeriodCalculator.GetPreviousWeekPeriod(DateTime.UtcNow);

            await _lockService.StartRefreshAsync(LockTtl);
            try
            {
                var currentSeason = await _seasonRepository.GetCurrent();
                var currentWeek = await _theBestWeekRepository.GetCurrentWeek();
                if(currentWeek != null)
                    await _theBestWeekRepository.Delete(currentWeek.Id);
                foreach (var city in cityNames)
                {
                    string normalizeCity = _normalizeCityNameService.Normalize(city);
                    var sortedSetKey = PhotoRedisKeys.RatingSortedSet(normalizeCity, currentSeason.Id);
                    var topIds = await _db.SortedSetRangeByRankAsync(
                        sortedSetKey,
                        start: 0,
                        order: Order.Descending  
                    );
                    if (await _theBestWeekRepository.ExistsAsync(city, period.Year, period.WeekOfYear, cancellationToken))
                        continue;
                    if (topIds == null)
                        continue;

                    var ids = topIds.Select(x => Guid.Parse(x.ToString())).ToList();
                    var photos = await _photoUserRepository.GetByIdsAsync(ids);
                    var snapshotJson = JsonSerializer.Serialize(photos);

                    var weekResult = Models.TheBestWeek.Create(
                        city,
                        period.Year,
                        period.WeekOfYear,
                        period.WeekLabel,
                        snapshotJson);

                    if (weekResult.IsFailure)
                        continue;
                    
                    await _theBestWeekRepository.Create(weekResult.Value);

                    _logger.LogInformation(
                        "Лучшая неделя {Year}-W{Week} для {City}: {Count} фото",
                        period.Year,
                        period.WeekOfYear,
                        city,
                        ids.Count);
                }
            }
            finally
            {
                await _lockService.EndRefreshAsync();
            }
        }
    }
}
