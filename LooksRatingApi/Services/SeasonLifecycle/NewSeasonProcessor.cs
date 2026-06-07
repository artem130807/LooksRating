using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Models;
using LooksRatingApi.Infrastructure.Quartz;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using StackExchange.Redis;

namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class NewSeasonProcessor : INewSeasonProcessor
    {
        private const int BatchSize = 5000;
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);

        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly ILoadingCityService _loadingCityService;
        private readonly INormalizeCityNameService _normalizeCityName;
        private readonly ArchivingLockService _lockService;
        private readonly ApplicationClock _clock;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<NewSeasonProcessor> _logger;

        public NewSeasonProcessor(
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository,
            IListSeasonsRepository listSeasonsRepository,
            ILoadingCityService loadingCityService,
            INormalizeCityNameService normalizeCityName,
            ArchivingLockService lockService,
            ApplicationClock clock,
            IConnectionMultiplexer redis,
            ILogger<NewSeasonProcessor> logger)
        {
            _photoProfileRepository = photoProfileRepository;
            _seasonRepository = seasonRepository;
            _listSeasonsRepository = listSeasonsRepository;
            _loadingCityService = loadingCityService;
            _normalizeCityName = normalizeCityName;
            _lockService = lockService;
            _clock = clock;
            _redis = redis;
            _logger = logger;
        }

        public async Task ProcessMonthlyRolloverAsync(CancellationToken cancellationToken)
        {
            var now = _clock.GetNow();
            _logger.LogInformation(
                "Смена сезона: старт (now={Now:O}, month={Month}, day={Day})",
                now,
                now.Month,
                now.Day);

            if (now.Day != 1)
            {
                _logger.LogInformation("Смена сезона пропущена: сегодня не 1-е число месяца");
                return;
            }

            var cityNames = _loadingCityService.GetCityNames();
            if (cityNames.Count == 0)
            {
                _logger.LogError("Список городов пуст, смена сезона невозможна");
                return;
            }

            var latestList = await _listSeasonsRepository.GetLatest(includeSeasons: false);
            if (latestList is null)
            {
                _logger.LogWarning("Смена сезона пропущена: глава не найдена");
                return;
            }

            await using var lockHandle = await _lockService.TryAcquireAsync(LockTtl, cancellationToken);
            if (lockHandle is null)
            {
                _logger.LogWarning("Смена сезона пропущена: архивация уже выполняется");
                return;
            }

            var listForNewSeason = latestList;
            Season? seasonToClose = null;

            if (now.Month == 1)
            {
                var previousList = await _listSeasonsRepository.GetPreviousToLatest();
                if (previousList is not null)
                    seasonToClose = await _seasonRepository.GetCurrentByList(previousList.Id);
            }
            else
            {
                seasonToClose = await _seasonRepository.GetCurrentByList(latestList.Id);
            }

            if (seasonToClose is not null)
            {
                var archivedCount = await ArchivePhotosAsync(seasonToClose.Id, cityNames, cancellationToken);
                seasonToClose.IsClosed = true;
                await _seasonRepository.Update(seasonToClose);
                await ClearCityCachesAsync(cityNames, seasonToClose.Id);
                _logger.LogInformation(
                    "Сезон {SeasonId} (№{Number}) закрыт, архивировано профилей: {Archived}",
                    seasonToClose.Id,
                    seasonToClose.Number,
                    archivedCount);
            }
            else
            {
                _logger.LogInformation("Смена сезона: закрывать текущий сезон не требуется");
            }

            var existingOpen = await _seasonRepository.GetCurrentByList(listForNewSeason.Id);
            if (existingOpen is not null && existingOpen.Number == now.Month && !existingOpen.IsClosed)
            {
                _logger.LogInformation(
                    "Смена сезона пропущена: открытый сезон {SeasonId} (№{Number}) уже существует",
                    existingOpen.Id,
                    existingOpen.Number);
                return;
            }

            var newSeasonResult = Season.Create(
                SeasonMonthNames.Get(now.Month),
                now.Month,
                listForNewSeason.Id);

            if (newSeasonResult.IsFailure)
            {
                _logger.LogWarning("Смена сезона пропущена: {Error}", newSeasonResult.Error);
                return;
            }

            await _seasonRepository.Create(newSeasonResult.Value);
            _logger.LogInformation(
                "Создан сезон {SeasonId} ({Number}) в главе {ListId}",
                newSeasonResult.Value.Id,
                newSeasonResult.Value.Number,
                listForNewSeason.Id);
        }

        private async Task<int> ArchivePhotosAsync(Guid seasonId, HashSet<string> cityNames, CancellationToken cancellationToken)
        {
            var skip = 0;
            var totalArchived = 0;
            while (true)
            {
                var ids = await _photoProfileRepository.GetProfileIdsBatchAsync(seasonId, skip, BatchSize, cancellationToken);
                if (ids.Count == 0)
                    break;

                await _photoProfileRepository.ArchiveProfilesAsync(ids, cancellationToken);
                totalArchived += ids.Count;
                skip += BatchSize;

                if (ids.Count < BatchSize)
                    break;
            }

            return totalArchived;
        }

        private async Task ClearCityCachesAsync(HashSet<string> cityNames, Guid seasonId)
        {
            var db = _redis.GetDatabase();
            foreach (var cityName in cityNames)
            {
                var cityKey = _normalizeCityName.Normalize(cityName);
                await db.KeyDeleteAsync(PhotoRedisKeys.RatingSortedSet(cityKey, seasonId));
                await db.KeyDeleteAsync($"photos:{cityKey}by_date");
            }

            _logger.LogInformation(
                "Смена сезона: очищен Redis-кэш для {CityCount} городов (сезон {SeasonId})",
                cityNames.Count,
                seasonId);
        }
    }
}
