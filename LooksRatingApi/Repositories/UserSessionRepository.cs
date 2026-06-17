using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly LooksRatingDbContext _context;

        public UserSessionRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public Task<UserSession?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
        {
            return _context.UserSessions
                .AsNoTracking()
                .Include(s => s.User)
                    .ThenInclude(u => u!.RecomendationSettings)
                .FirstOrDefaultAsync(s => s.TelegramId == telegramId, cancellationToken);
        }

        public Task<UserSession?> GetByTelegramIdForUpdateAsync(long telegramId, CancellationToken cancellationToken = default)
        {
            return _context.UserSessions
                .FirstOrDefaultAsync(s => s.TelegramId == telegramId, cancellationToken);
        }

        public Task<bool> ExistsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
        {
            return _context.UserSessions.AnyAsync(s => s.TelegramId == telegramId, cancellationToken);
        }

        public async Task CreateAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
