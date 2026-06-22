using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services.TheBestWeek
{
    public sealed class TheBestWeekTopStatsService : ITheBestWeekTopStatsService
    {
        private readonly ITheBestWeekRepository _theBestWeekRepository;

        public TheBestWeekTopStatsService(ITheBestWeekRepository theBestWeekRepository)
        {
            _theBestWeekRepository = theBestWeekRepository;
        }

        public async Task<List<long>> GetCurrentWeekTopTelegramIdsAsync(CancellationToken cancellationToken = default)
        {
            var weekRecords = await _theBestWeekRepository.GetLatestWeekSnapshotRecordsAsync(cancellationToken);
            return TheBestWeekTopTelegramIdsCollector
                .CollectForWeekRecords(weekRecords)
                .OrderBy(x => x)
                .ToList();
        }

        public async Task<int> CountWeekAppearancesForTelegramIdAsync(
            long telegramId,
            CancellationToken cancellationToken = default)
        {
            if (telegramId <= 0)
            {
                return 0;
            }

            var groupedWeeks = await _theBestWeekRepository.GetAllWeekSnapshotRecordsGroupedAsync(cancellationToken);
            var appearances = 0;

            foreach (var weekRecords in groupedWeeks)
            {
                var topIds = TheBestWeekTopTelegramIdsCollector.CollectForWeekRecords(weekRecords);
                if (topIds.Contains(telegramId))
                {
                    appearances++;
                }
            }

            return appearances;
        }
    }
}
