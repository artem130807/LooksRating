using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.UserSessionContracts
{
    public interface IUserSessionRepository
    {
        Task<UserSession?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
        Task<UserSession?> GetByTelegramIdForUpdateAsync(long telegramId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
        Task CreateAsync(UserSession session, CancellationToken cancellationToken = default);
        Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    }
}
