using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services.TheBestWeek;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LooksRatingApi.Repositories
{
    public class TheBestWeekRepository : ITheBestWeekRepository
    {
        private readonly LooksRatingDbContext _context;
        private readonly IMemoryCache _memoryCache;
        public TheBestWeekRepository(LooksRatingDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        public async Task Create(TheBestWeek theBestWeek)
        {
            _context.TheBestWeeks.Add(theBestWeek);
            await _context.SaveChangesAsync();
        }


        public async Task Delete(Guid id)
        {
            await _context.TheBestWeeks.Where(x => x.Id == id).ExecuteDeleteAsync();
        }

        public async Task Update(TheBestWeek theBestWeek)
        {
            _context.TheBestWeeks.Update(theBestWeek);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string city, int year, int weekOfYear, CancellationToken cancellationToken)
        {
            return await _context.TheBestWeeks.AnyAsync(
                w => w.City == city && w.Year == year && w.WeekOfYear == weekOfYear,
                cancellationToken);
        }

        public async Task<TheBestWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.TheBestWeeks
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }

        public async Task<TheBestWeek?> GetByCityYearWeekAsync(
            string city,
            int year,
            int weekOfYear,
            CancellationToken cancellationToken)
        {
            return await _context.TheBestWeeks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    w => w.City == city && w.Year == year && w.WeekOfYear == weekOfYear,
                    cancellationToken);
        }

        public async Task<List<TheBestWeek>> GetByCityAsync(
            string city,
            int? year,
            int? weekOfYear,
            int limit,
            CancellationToken cancellationToken)
        {
            var query = _context.TheBestWeeks
                .AsNoTracking()
                .Where(w => w.City == city);

            if (year.HasValue)
            {
                query = query.Where(w => w.Year == year.Value);
            }

            if (weekOfYear.HasValue)
            {
                query = query.Where(w => w.WeekOfYear == weekOfYear.Value);
            }

            return await query
                .OrderByDescending(w => w.Year)
                .ThenByDescending(w => w.WeekOfYear)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<long>> GetIds()
        {
            var weekRecords = await GetLatestWeekSnapshotRecordsAsync();
            return TheBestWeekTopTelegramIdsCollector
                .CollectForWeekRecords(weekRecords)
                .OrderBy(x => x)
                .ToList();
        }

        public async Task<List<TheBestWeekWeekRecord>> GetLatestWeekSnapshotRecordsAsync(
            CancellationToken cancellationToken = default)
        {
            var latest = await GetCurrentWeek();
            if (latest is null)
            {
                return [];
            }

            var records = await _context.TheBestWeeks
                .AsNoTracking()
                .Where(w => w.Year == latest.Year && w.WeekOfYear == latest.WeekOfYear)
                .ToListAsync(cancellationToken);

            return ToWeekRecords(records);
        }

        public async Task<List<List<TheBestWeekWeekRecord>>> GetAllWeekSnapshotRecordsGroupedAsync(
            CancellationToken cancellationToken = default)
        {
            var records = await _context.TheBestWeeks
                .AsNoTracking()
                .OrderBy(w => w.Year)
                .ThenBy(w => w.WeekOfYear)
                .ThenBy(w => w.City)
                .ToListAsync(cancellationToken);

            return records
                .GroupBy(w => (w.Year, w.WeekOfYear))
                .Select(group => ToWeekRecords(group.ToList()))
                .ToList();
        }

        private static List<TheBestWeekWeekRecord> ToWeekRecords(IReadOnlyList<TheBestWeek> records)
        {
            return records
                .Select(record => new TheBestWeekWeekRecord(
                    record.City,
                    TheBestWeekSnapshotSerializer.Deserialize(record.SnapshotJson)))
                .ToList();
        }

        public async Task<TheBestWeek?> GetCurrentWeek()
        {
            return await _context.TheBestWeeks.OrderByDescending(w => w.CreatedDate).FirstOrDefaultAsync();
        }

    }
}
