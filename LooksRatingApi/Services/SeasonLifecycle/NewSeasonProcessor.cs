using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class NewSeasonProcessor : INewSeasonProcessor
    {
        private const int BatchSize = 5000;
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);

        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly INormalizeCityNameService _normalizeCityName;
        private readonly ArchivingLockService _lockService;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<NewSeasonProcessor> _logger;

        public NewSeasonProcessor(
            IPhotoUserRepository photoUserRepository,
            ISeasonRepository seasonRepository,
            IListSeasonsRepository listSeasonsRepository,
            IMemoryCache memoryCache,
            INormalizeCityNameService normalizeCityName,
            ArchivingLockService lockService,
            IConnectionMultiplexer redis,
            ILogger<NewSeasonProcessor> logger)
        {
            _photoUserRepository = photoUserRepository;
            _seasonRepository = seasonRepository;
            _listSeasonsRepository = listSeasonsRepository;
            _memoryCache = memoryCache;
            _normalizeCityName = normalizeCityName;
            _lockService = lockService;
            _redis = redis;
            _logger = logger;
        }

        public async Task ProcessMonthlyRolloverAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            if (now.Day != 1)
                return;

            if (!_memoryCache.TryGetValue<HashSet<string>>("key_cities_names", out var cityNames) || cityNames is null)
            {
                _logger.LogWarning("Список городов не загружен, смена сезона отложена");
                return;
            }

            var latestList = await _listSeasonsRepository.GetLatest(includeSeasons: false);
            if (latestList is null)
                return;

            if (await _lockService.IsArchivingInProgressAsync())
            {
                _logger.LogWarning("Смена сезона пропущена: идёт архивация");
                return;
            }

            await _lockService.StartArchivingAsync(LockTtl);
            try
            {
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
                    await ArchivePhotosAsync(seasonToClose.Id, cityNames, cancellationToken);
                    seasonToClose.IsClosed = true;
                    await _seasonRepository.Update(seasonToClose);
                    await ClearCityCachesAsync(cityNames, seasonToClose.Id);
                }

                var existingOpen = await _seasonRepository.GetCurrentByList(listForNewSeason.Id);
                if (existingOpen is not null && existingOpen.Number == now.Month && !existingOpen.IsClosed)
                    return;

                var newSeasonResult = Season.Create(
                    SeasonMonthNames.Get(now.Month),
                    now.Month,
                    listForNewSeason.Id);

                if (newSeasonResult.IsFailure)
                    return;

                await _seasonRepository.Create(newSeasonResult.Value);
                _logger.LogInformation(
                    "Создан сезон {SeasonId} ({Number}) в главе {ListId}",
                    newSeasonResult.Value.Id,
                    newSeasonResult.Value.Number,
                    listForNewSeason.Id);
            }
            finally
            {
                await _lockService.EndArchivingAsync();
            }
        }

        private async Task ArchivePhotosAsync(Guid seasonId, HashSet<string> cityNames, CancellationToken cancellationToken)
        {
            var skip = 0;
            while (true)
            {
                var ids = await _photoUserRepository.GetPhotoIdsBatch(seasonId, skip, BatchSize);
                if (ids.Count == 0)
                    break;

                await _photoUserRepository.ExecuteUpdateAsync(ids);
                skip += BatchSize;

                if (ids.Count < BatchSize)
                    break;
            }
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
        }
    }
}
