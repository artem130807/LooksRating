using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace LooksRatingApi.Services.BackGroundServices.Handlers
{
    public class AddPhotoUsersCacheHandler : IAddPhotoUsersCacheHandler
    {
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<AddPhotoUsersCacheHandler> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly IMemoryCache _memoryCache;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly ISeasonRepository _seasonRepository;
        public AddPhotoUsersCacheHandler(LooksRatingDbContext context ,ILogger<AddPhotoUsersCacheHandler> logger,  IConnectionMultiplexer redis, IMemoryCache memoryCache, INormalizeCityNameService normalizeCityNameService, ISeasonRepository seasonRepository)
        {
            _context = context;
            _logger = logger;
            _redis = redis;
            _db = redis.GetDatabase();
            _memoryCache = memoryCache;
            _normalizeCityNameService = normalizeCityNameService;
            _seasonRepository = seasonRepository;
        }
        public async Task Handle(CancellationToken cancellationToken)
        {
            var lockKey = "photos:update_lock";
            var lockToken = Guid.NewGuid().ToString();
            bool lockAcquired = await _db.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(30));
            if (!lockAcquired)
            {
                _logger.LogInformation("Обновление SortedSet уже выполняется другим экземпляром");
                return;
            }
            try
            {
                var season = await _seasonRepository.GetCurrent();
                if (season is null)
                {
                    _logger.LogInformation("Текущий сезон не найден — пропуск обновления кэша");
                    return;
                }

                var tempKey = "photos:by_date_temp";
                var tempRatingKey = "photos:by_rating_temp";
                var cacheKey  = "key_cities_names";
                if (!_memoryCache.TryGetValue<HashSet<string>>(cacheKey, out var cityNames) || cityNames is null)
                {
                    return;
                }
                const int batchSize = 5000;
                foreach (var cityName in cityNames)
                {
                    await _db.KeyDeleteAsync(tempKey);
                    var skip = 0;
                    var hasMore = true;
                    var totalForCity = 0;
                    while (hasMore)
                    {
                        var batch = await _context.PhotoUsers
                            .AsNoTracking()
                            .Where(p => p.CityNomination.Value == cityName)
                            .OrderByDescending(p => p.CreatedAt)
                            .Skip(skip)
                            .Take(batchSize)
                            .Select(p => new { p.Id, p.CreatedAt })
                            .ToListAsync(cancellationToken);

                        if (batch.Count == 0)
                            break;

                        var entries = batch.Select(p => new SortedSetEntry(
                            p.Id.ToString(),
                            p.CreatedAt.Ticks
                        )).ToArray();

                        await _db.SortedSetAddAsync(tempKey, entries);

                        totalForCity += batch.Count;
                        skip += batchSize;
                        hasMore = batch.Count == batchSize;
                    }

                    var cityKey = _normalizeCityNameService.Normalize(cityName);
                    var destKey = PhotoRedisKeys.RatingSortedSet(cityKey, season.Id);
                    if (await _db.KeyExistsAsync(tempKey))
                        await _db.KeyRenameAsync(tempKey, destKey);
                    else
                        await _db.KeyDeleteAsync(destKey);

                    await _db.KeyDeleteAsync(tempRatingKey);
                    var skipRating = 0;
                    var hasMoreRating = true;
                    while (hasMoreRating)
                    {
                        var ratingBatch = await _context.PhotoUsers
                            .AsNoTracking()
                            .Where(p => p.CityNomination.Value == cityName)
                            .OrderByDescending(p => p.Rating)
                            .ThenByDescending(p => p.RatingCount)
                            .Skip(skipRating)
                            .Take(batchSize)
                            .Select(p => new { p.Id, p.Rating, p.RatingCount })
                            .ToListAsync(cancellationToken);

                        if (ratingBatch.Count == 0)
                            break;

                        var ratingEntries = ratingBatch.Select(p => new SortedSetEntry(
                            p.Id.ToString(),
                            PhotoRankingScore.ToSortScore(p.Rating, p.RatingCount)
                        )).ToArray();

                        await _db.SortedSetAddAsync(tempRatingKey, ratingEntries);

                        skipRating += batchSize;
                        hasMoreRating = ratingBatch.Count == batchSize;
                    }

                    var destRatingKey = PhotoRedisKeys.RatingSortedSet(cityKey, season.Id);
                    if (await _db.KeyExistsAsync(tempRatingKey))
                        await _db.KeyRenameAsync(tempRatingKey, destRatingKey);
                    else
                        await _db.KeyDeleteAsync(destRatingKey);

                    _logger.LogInformation("Sorted Set обновлён для города {City}, фото: {Count}", cityName, totalForCity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении SortedSet");
            }
            finally
            {
                await _db.LockReleaseAsync(lockKey, lockToken);
            }
        }
    }
}