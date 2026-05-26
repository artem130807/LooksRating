using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.RecomendationSettingsContracts
{
    public interface IRecomendationSettingsRepository
    {
        Task<RecomendationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<RecomendationSettings?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
        Task CreateAsync(RecomendationSettings settings, CancellationToken cancellationToken = default);
        Task UpdateAsync(RecomendationSettings settings, CancellationToken cancellationToken = default);
        Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
