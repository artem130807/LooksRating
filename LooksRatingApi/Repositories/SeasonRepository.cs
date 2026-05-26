using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class SeasonRepository : ISeasonRepository
    {
        private readonly LooksRatingDbContext _context;

        public SeasonRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task Create(Season season)
        {
            _context.Seasons.Add(season);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            await _context.Seasons
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync();
        }

        public async Task Update(Season season)
        {
            _context.Seasons.Update(season);
            await _context.SaveChangesAsync();
        }

        public async Task<Season?> GetById(Guid id)
        {
            return await _context.Seasons
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Season?> GetByIdWithChapterAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Seasons
                .AsNoTracking()
                .Include(s => s.ListSeasons)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Season?> GetByNumber(int number)
        {
            return await _context.Seasons
                .FirstOrDefaultAsync(s => s.Number == number);
        }

        public async Task<Season?> GetCurrent()
        {
            return await _context.Seasons
                .Where(s => !s.IsClosed)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Season?> GetCurrentByList(Guid listId)
        {
            return await _context.Seasons
                .Where(s => s.ListSeasonsId == listId && !s.IsClosed)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Season>> GetSeasons(bool includeClosed = true)
        {
            var query = _context.Seasons.AsQueryable();

            if (!includeClosed)
                query = query.Where(s => !s.IsClosed);

            return await query
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Season>> GetByListSeasonsIdAsync(
            Guid listSeasonsId,
            bool includeClosed = true,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Seasons
                .AsNoTracking()
                .Where(s => s.ListSeasonsId == listSeasonsId);

            if (!includeClosed)
                query = query.Where(s => !s.IsClosed);

            return await query
                .OrderBy(s => s.Number)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetPhotoCountsBySeasonIdsAsync(
            IEnumerable<Guid> seasonIds,
            CancellationToken cancellationToken = default)
        {
            var ids = seasonIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, int>();

            return await _context.PhotoUsers
                .AsNoTracking()
                .Where(p => ids.Contains(p.SeasonId))
                .GroupBy(p => p.SeasonId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }
    }
}
