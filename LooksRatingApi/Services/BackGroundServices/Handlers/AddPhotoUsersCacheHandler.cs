using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace LooksRatingApi.Services.BackGroundServices.Handlers
{
    public class AddPhotoUsersCacheHandler : IAddPhotoUsersCacheHandler
    {
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<AddPhotoUsersCacheHandler> _logger;
        private readonly IDatabase _db;
        private readonly IMemoryCache _memoryCache;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly ISeasonRepository _seasonRepository;
        public AddPhotoUsersCacheHandler(LooksRatingDbContext context ,ILogger<AddPhotoUsersCacheHandler> logger,  IConnectionMultiplexer redis, IMemoryCache memoryCache, INormalizeCityNameService normalizeCityNameService, ISeasonRepository seasonRepository)
        {
            _context = context;
            _logger = logger;
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

                var cacheKey = CityNamesCacheKeys.Names;
                if (!_memoryCache.TryGetValue<HashSet<string>>(cacheKey, out var cityNames) || cityNames is null)
                {
                    return;
                }
                const int batchSize = 5000;
                foreach (var cityName in cityNames)
                {
                    var normalizedCity = _normalizeCityNameService.Normalize(cityName);
                    var destRatingKey = PhotoRedisKeys.RatingSortedSet(normalizedCity, season.Id);
                    var tempRatingKey = $"profiles:rating:temp:{normalizedCity}:{season.Id:N}";
                    await _db.KeyDeleteAsync(tempRatingKey);

                    var skipRating = 0;
                    var hasMoreRating = true;
                    var totalForCity = 0;
                    while (hasMoreRating)
                    {
                        var ratingBatch = await _context.PhotoProfiles
                            .AsNoTracking()
                            .Where(p => p.SeasonId == season.Id)
                            .Where(p => p.Status == StatusEnum.Active)
                            .Where(p => p.CityNomination.Value == cityName)
                            .OrderByDescending(p => p.RatingCount > 0 ? 1 : 0)
                            .ThenByDescending(p => p.RatingCount > 0
                                ? ((p.Rating * p.RatingCount) + (PhotoRankingScore.PriorMean * PhotoRankingScore.PriorWeight))
                                    / (p.RatingCount + PhotoRankingScore.PriorWeight)
                                : PhotoRankingScore.UnratedScore)
                            .ThenByDescending(p => p.Rating)
                            .ThenByDescending(p => p.RatingCount)
                            .ThenByDescending(p => p.CreatedAt)
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

                        totalForCity += ratingBatch.Count;
                        skipRating += batchSize;
                        hasMoreRating = ratingBatch.Count == batchSize;
                    }

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