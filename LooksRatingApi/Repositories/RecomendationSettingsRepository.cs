using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class RecomendationSettingsRepository : IRecomendationSettingsRepository
    {
        private readonly LooksRatingDbContext _context;

        public RecomendationSettingsRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task<RecomendationSettings?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<RecomendationSettings>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<RecomendationSettings?> GetByTelegramIdAsync(
            long telegramId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<RecomendationSettings>()
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.User.TelegramId == telegramId, cancellationToken);
        }

        public async Task CreateAsync(RecomendationSettings settings, CancellationToken cancellationToken = default)
        {
            _context.Set<RecomendationSettings>().Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(RecomendationSettings settings, CancellationToken cancellationToken = default)
        {
            _context.Set<RecomendationSettings>().Update(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _context.Set<RecomendationSettings>()
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
