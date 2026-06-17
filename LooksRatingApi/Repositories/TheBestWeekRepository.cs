using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
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
            var ids = new List<long>();
            const string CityNamesCacheKey = CityNamesCacheKeys.Names;
            if (!_memoryCache.TryGetValue<HashSet<string>>(CityNamesCacheKey, out var cityNames) || cityNames is null)
            {
                return new List<long>();
            }
            var currentTheBestWeek = await _context.TheBestWeeks.OrderByDescending(w => w.CreatedDate).FirstOrDefaultAsync();
            if(currentTheBestWeek == null)
                return new List<long>();
            var snapshotItems = TheBestWeekSnapshotSerializer.Deserialize(currentTheBestWeek.SnapshotJson);
            if (snapshotItems.Count == 0)
                return new List<long>();
            
            var profilesM = snapshotItems.Where(x => x.GenderNomination == Enums.GenderEnum.Male).ToList();
            var profilesG = snapshotItems.Where(x => x.GenderNomination == Enums.GenderEnum.Female).ToList();
            var ageList = TopService.GetIntsList();
            foreach(var city in cityNames)
            {
                foreach(var age in ageList)
                {
                     var filteredM = profilesM
                    .Where(p => p.City == city && age.Contains(p.AgeNomination))
                    .ToList();
                
                    var filteredG = profilesG
                        .Where(p => p.City == city && age.Contains(p.AgeNomination))
                        .ToList();
                    
                    var top10M = filteredM
                        .OrderByDescending(p => p.RatingCount > 0 ? 1 : 0)
                        .ThenByDescending(p => PhotoRankingScore.ToRankScore(p.Rating, p.RatingCount))
                        .ThenByDescending(p => p.Rating)
                        .ThenByDescending(p => p.RatingCount)
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(10)
                        .ToList();
                    
                    var top10G = filteredG
                        .OrderByDescending(p => p.RatingCount > 0 ? 1 : 0)
                        .ThenByDescending(p => PhotoRankingScore.ToRankScore(p.Rating, p.RatingCount))
                        .ThenByDescending(p => p.Rating)
                        .ThenByDescending(p => p.RatingCount)
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(10)
                        .ToList();
                    
                    ids.AddRange(top10M
                        .Select(p => p.TelegramId)
                        .Where(x => x > 0));
                    ids.AddRange(top10G
                        .Select(p => p.TelegramId)
                        .Where(x => x > 0));
                }
            }
            var uniqueIds = ids.Distinct().ToList();
            return uniqueIds;
        }

        public async Task<TheBestWeek?> GetCurrentWeek()
        {
            return await _context.TheBestWeeks.OrderByDescending(w => w.CreatedDate).FirstOrDefaultAsync();
        }

    }
}
