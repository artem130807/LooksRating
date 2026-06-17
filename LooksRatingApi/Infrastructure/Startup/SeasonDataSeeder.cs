using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SeasonLifecycle;

namespace LooksRatingApi.Infrastructure.Startup
{
    public sealed class SeasonDataSeeder : ISeasonDataSeeder
    {
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ILogger<SeasonDataSeeder> _logger;

        public SeasonDataSeeder(
            IListSeasonsRepository listSeasonsRepository,
            ISeasonRepository seasonRepository,
            ILogger<SeasonDataSeeder> logger)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _seasonRepository = seasonRepository;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (await _seasonRepository.GetCurrent() is not null)
            {
                _logger.LogInformation("Сид сезона пропущен: открытый сезон уже существует");
                return;
            }

            var chapter = await _listSeasonsRepository.GetLatest(includeSeasons: false);
            if (chapter is null)
            {
                var chapterResult = ListSeasons.Create();
                if (chapterResult.IsFailure)
                {
                    _logger.LogWarning("Сид сезона пропущен: не удалось создать главу");
                    return;
                }

                await _listSeasonsRepository.Create(chapterResult.Value);
                chapter = chapterResult.Value;
                _logger.LogInformation("Создана начальная глава {ChapterId}", chapter.Id);
            }

            var openInChapter = await _seasonRepository.GetCurrentByList(chapter.Id);
            if (openInChapter is not null)
            {
                _logger.LogInformation("Сид сезона пропущен: в главе уже есть открытый сезон");
                return;
            }

            var month = DateTime.UtcNow.Month;
            var seasonResult = Season.Create(
                SeasonMonthNames.Get(month),
                month,
                chapter.Id);

            if (seasonResult.IsFailure)
            {
                _logger.LogWarning("Сид сезона пропущен: {Error}", seasonResult.Error);
                return;
            }

            await _seasonRepository.Create(seasonResult.Value);
            _logger.LogInformation(
                "Создан начальный сезон {SeasonId} ({Name}) в главе {ChapterId}",
                seasonResult.Value.Id,
                seasonResult.Value.Name,
                chapter.Id);
        }
    }
}
