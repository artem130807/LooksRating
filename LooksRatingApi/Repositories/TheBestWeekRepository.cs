using System.ComponentModel;
using System.Text.Json;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

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
            return new List<TheBestWeek>();
        }
        public async Task<List<long>> GetIds()
        {
            var ids = new List<long>();
            string CityNamesCacheKey = "key_cities_names";
            if (!_memoryCache.TryGetValue<HashSet<string>>(CityNamesCacheKey, out var cityNames) || cityNames is null)
            {
                return new List<long>();
            }
            var currentTheBestWeek = await _context.TheBestWeeks.OrderByDescending(w => w.CreatedDate).FirstOrDefaultAsync();
            if(currentTheBestWeek == null)
                return new List<long>();
            var jsonPhotos = currentTheBestWeek.SnapshotJson;
            var photos = JsonSerializer.Deserialize<List<PhotoUser>>(jsonPhotos);
            if(photos == null)
                return new List<long>();
            
            var photosM = photos.Where(x => x.GenderNomination == Enums.GenderEnum.Male).ToList();
            var photosG = photos.Where(x => x.GenderNomination == Enums.GenderEnum.Female).ToList();
            var ageList = TopService.GetIntsList();
            foreach(var city in cityNames)
            {
                foreach(var age in ageList)
                {
                     var filteredM = photosM
                    .Where(p => p.CityNomination.Value == city && age.Contains(p.AgeNomination))
                    .ToList();
                
                    var filteredG = photosG
                        .Where(p => p.CityNomination.Value == city && age.Contains(p.AgeNomination))
                        .ToList();
                    
                    var top10M = filteredM
                        .OrderByDescending(p => p.Rating * p.RatingCount)
                        .Take(10)
                        .ToList();
                    
                    var top10G = filteredG
                        .OrderByDescending(p => p.Rating * p.RatingCount)
                        .Take(10)
                        .ToList();
                    
                    ids.AddRange(top10M.Select(p => p.User.TelegramId));
                    ids.AddRange(top10G.Select(p => p.User.TelegramId));
                }
            }
            var uniqueIds = ids.Distinct().ToList();
            return uniqueIds;
        }

        public async Task<TheBestWeek> GetCurrentWeek()
        {
            return await _context.TheBestWeeks.OrderByDescending(w => w.CreatedDate).FirstOrDefaultAsync();
        }

    }
}
