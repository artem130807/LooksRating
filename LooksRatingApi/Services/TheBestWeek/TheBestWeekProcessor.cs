using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Infrastructure.Quartz;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Models;
using StackExchange.Redis;

namespace LooksRatingApi.Services.TheBestWeek
{
    public sealed class TheBestWeekProcessor : ITheBestWeekProcessor
    {
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(5);
        private readonly ITheBestWeekRepository _theBestWeekRepository;
        private readonly ILoadingCityService _loadingCityService;
        private readonly TheBestWeekLockService _lockService;
        private readonly ArchivingLockService _archivingLockService;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ILogger<TheBestWeekProcessor> _logger;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly StackExchange.Redis.IDatabase _db;
        private readonly IConnectionMultiplexer _redis;
        private readonly ApplicationClock _clock;

        public TheBestWeekProcessor(
            ITheBestWeekRepository theBestWeekRepository,
            ILoadingCityService loadingCityService,
            TheBestWeekLockService lockService,
            ArchivingLockService archivingLockService,
            ILogger<TheBestWeekProcessor> logger,
            ISeasonRepository seasonRepository,
            INormalizeCityNameService normalizeCityNameService,
            IConnectionMultiplexer redis,
            IPhotoProfileRepository photoProfileRepository,
            ApplicationClock clock)
        {
            _theBestWeekRepository = theBestWeekRepository;
            _loadingCityService = loadingCityService;
            _lockService = lockService;
            _archivingLockService = archivingLockService;
            _logger = logger;
            _seasonRepository = seasonRepository;
            _normalizeCityNameService = normalizeCityNameService;
            _redis = redis;
            _db = _redis.GetDatabase();
            _photoProfileRepository = photoProfileRepository;
            _clock = clock;
        }

        public async Task RefreshWeeklyAsync(CancellationToken cancellationToken)
        {
            var period = WeekPeriodCalculator.GetPreviousWeekPeriod(_clock.GetNow());
            _logger.LogInformation(
                "Лучшая неделя: старт обновления за {Year}-W{Week} ({Label})",
                period.Year,
                period.WeekOfYear,
                period.WeekLabel);

            var cityNames = _loadingCityService.GetCityNames();
            if (cityNames.Count == 0)
            {
                _logger.LogError("Список городов пуст, обновление лучшей недели невозможно");
                return;
            }

            if (await _archivingLockService.IsArchivingInProgressAsync())
            {
                _logger.LogWarning("Обновление лучшей недели пропущено: идёт архивация сезона");
                return;
            }

            await using var lockHandle = await _lockService.TryAcquireAsync(LockTtl, cancellationToken);
            if (lockHandle is null)
            {
                _logger.LogWarning("Обновление лучшей недели пропущено: уже выполняется");
                return;
            }

            var currentSeason = await _seasonRepository.GetCurrent();
            if (currentSeason is null)
            {
                _logger.LogWarning("Текущий сезон не найден, обновление лучшей недели пропущено");
                return;
            }

            var currentWeek = await _theBestWeekRepository.GetCurrentWeek();
            if (currentWeek != null)
            {
                await _theBestWeekRepository.Delete(currentWeek.Id);
                _logger.LogInformation("Лучшая неделя: удалена предыдущая запись {WeekId}", currentWeek.Id);
            }

            var created = 0;
            var skippedExists = 0;
            var skippedEmpty = 0;
            var skippedError = 0;

            foreach (var city in cityNames)
            {
                var normalizeCity = _normalizeCityNameService.Normalize(city);
                var sortedSetKey = PhotoRedisKeys.RatingSortedSet(normalizeCity, currentSeason.Id);
                var topIds = await _db.SortedSetRangeByRankAsync(
                    sortedSetKey,
                    start: 0,
                    order: Order.Descending);

                if (await _theBestWeekRepository.ExistsAsync(city, period.Year, period.WeekOfYear, cancellationToken))
                {
                    skippedExists++;
                    continue;
                }

                if (topIds == null || topIds.Length == 0)
                {
                    skippedEmpty++;
                    _logger.LogDebug(
                        "Лучшая неделя: Redis пуст для {City} (ключ {Key})",
                        city,
                        sortedSetKey);
                    continue;
                }

                var ids = topIds.Select(x => Guid.Parse(x.ToString())).ToList();
                var profiles = await _photoProfileRepository.GetByIdsAsync(ids);
                var snapshotJson = TheBestWeekSnapshotSerializer.Serialize(profiles);

                var weekResult = Models.TheBestWeek.Create(
                    city,
                    period.Year,
                    period.WeekOfYear,
                    period.WeekLabel,
                    snapshotJson);

                if (weekResult.IsFailure)
                {
                    skippedError++;
                    _logger.LogWarning(
                        "Лучшая неделя: не создана для {City}: {Error}",
                        city,
                        weekResult.Error);
                    continue;
                }

                await _theBestWeekRepository.Create(weekResult.Value);
                created++;

                _logger.LogInformation(
                    "Лучшая неделя {Year}-W{Week} для {City}: {Count} фото",
                    period.Year,
                    period.WeekOfYear,
                    city,
                    ids.Count);
            }

            _logger.LogInformation(
                "Лучшая неделя: итог — создано {Created}, уже было {SkippedExists}, пустой Redis {SkippedEmpty}, ошибки {SkippedError}",
                created,
                skippedExists,
                skippedEmpty,
                skippedError);
        }
    }
}
