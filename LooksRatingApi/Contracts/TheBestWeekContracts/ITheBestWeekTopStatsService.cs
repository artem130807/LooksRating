namespace LooksRatingApi.Contracts.TheBestWeekContracts
{
    public interface ITheBestWeekTopStatsService
    {
        Task<List<long>> GetCurrentWeekTopTelegramIdsAsync(CancellationToken cancellationToken = default);

        Task<int> CountWeekAppearancesForTelegramIdAsync(
            long telegramId,
            CancellationToken cancellationToken = default);
    }
}
