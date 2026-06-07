using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Infrastructure.Quartz;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class NewListSeasonProcessor : INewListSeasonProcessor
    {
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);

        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ArchivingLockService _lockService;
        private readonly ApplicationClock _clock;
        private readonly ILogger<NewListSeasonProcessor> _logger;

        public NewListSeasonProcessor(
            IListSeasonsRepository listSeasonsRepository,
            ISeasonRepository seasonRepository,
            ArchivingLockService lockService,
            ApplicationClock clock,
            ILogger<NewListSeasonProcessor> logger)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _seasonRepository = seasonRepository;
            _lockService = lockService;
            _clock = clock;
            _logger = logger;
        }

        public async Task<bool> TryCreateNewChapterAsync(CancellationToken cancellationToken)
        {
            var now = _clock.GetNow();
            _logger.LogInformation(
                "Создание главы: старт (now={Now:O}, month={Month}, day={Day})",
                now,
                now.Month,
                now.Day);

            if (now.Month != 1 || now.Day != 1)
            {
                _logger.LogInformation("Создание главы пропущено: не 1 января");
                return false;
            }

            var list = await _listSeasonsRepository.GetLatest(includeSeasons: false);
            if (list is null)
            {
                _logger.LogWarning("Создание главы пропущено: текущая глава не найдена");
                return false;
            }

            var currentSeason = await _seasonRepository.GetCurrentByList(list.Id);
            if (currentSeason is null || currentSeason.Number != 12)
            {
                _logger.LogWarning(
                    "Создание главы пропущено: нужен 12-й сезон (текущий: {Number})",
                    currentSeason?.Number);
                return false;
            }

            await using var lockHandle = await _lockService.TryAcquireAsync(LockTtl, cancellationToken);
            if (lockHandle is null)
            {
                _logger.LogWarning("Создание главы пропущено: архивация уже выполняется");
                return false;
            }

            var newListResult = ListSeasons.Create();
            if (newListResult.IsFailure)
            {
                _logger.LogWarning("Создание главы пропущено: {Error}", newListResult.Error);
                return false;
            }

            await _listSeasonsRepository.Create(newListResult.Value);
            _logger.LogInformation("Создана новая глава {ListId}", newListResult.Value.Id);
            return true;
        }
    }
}
