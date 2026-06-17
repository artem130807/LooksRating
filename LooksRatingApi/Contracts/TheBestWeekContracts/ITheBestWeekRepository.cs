using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.TheBestWeekContracts
{
    public interface ITheBestWeekRepository
    {
        Task Create(TheBestWeek theBestWeek);
        Task Delete(Guid id);
        Task Update(TheBestWeek theBestWeek);
        Task<bool> ExistsAsync(string city, int year, int weekOfYear, CancellationToken cancellationToken);
        Task<TheBestWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<TheBestWeek?> GetByCityYearWeekAsync(string city, int year, int weekOfYear, CancellationToken cancellationToken);
        Task<List<TheBestWeek>> GetByCityAsync(string city, int? year, int? weekOfYear, int limit, CancellationToken cancellationToken);
        Task<List<long>> GetIds();
        Task<TheBestWeek?> GetCurrentWeek();
    }
}
