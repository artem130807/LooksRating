using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class NewListSeasonProcessor : INewListSeasonProcessor
    {
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);

        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ArchivingLockService _lockService;
        private readonly ILogger<NewListSeasonProcessor> _logger;

        public NewListSeasonProcessor(
            IListSeasonsRepository listSeasonsRepository,
            ISeasonRepository seasonRepository,
            ArchivingLockService lockService,
            ILogger<NewListSeasonProcessor> logger)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _seasonRepository = seasonRepository;
            _lockService = lockService;
            _logger = logger;
        }

        public async Task<bool> TryCreateNewChapterAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            if (now.Month != 1 || now.Day != 1)
                return false;

            var list = await _listSeasonsRepository.GetLatest(includeSeasons: false);
            if (list is null)
                return false;

            var currentSeason = await _seasonRepository.GetCurrentByList(list.Id);
            if (currentSeason is null || currentSeason.Number != 12)
                return false;

            if (await _lockService.IsArchivingInProgressAsync())
            {
                _logger.LogWarning("Создание главы пропущено: идёт архивация");
                return false;
            }

            await _lockService.StartArchivingAsync(LockTtl);
            try
            {
                var newListResult = ListSeasons.Create();
                if (newListResult.IsFailure)
                    return false;

                await _listSeasonsRepository.Create(newListResult.Value);
                _logger.LogInformation("Создана новая глава {ListId}", newListResult.Value.Id);
                return true;
            }
            finally
            {
                await _lockService.EndArchivingAsync();
            }
        }
    }
}
