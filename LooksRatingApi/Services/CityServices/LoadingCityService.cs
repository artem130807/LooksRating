using System.Text.Json;
using LooksRatingApi.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace LooksRatingApi.Services.CityServices
{
    public sealed class LoadingCityService : ILoadingCityService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LoadingCityService> _logger;
        private readonly object _sync = new();

        public LoadingCityService(
            IMemoryCache memoryCache,
            IWebHostEnvironment environment,
            ILogger<LoadingCityService> logger)
        {
            _memoryCache = memoryCache;
            _environment = environment;
            _logger = logger;
        }

        public HashSet<string> CreateCityNames(IWebHostEnvironment env) => LoadFromFile(env);

        public HashSet<string> GetCityNames()
        {
            if (TryGetCached(out var cached))
                return cached;

            lock (_sync)
            {
                if (TryGetCached(out cached))
                    return cached;

                _logger.LogWarning("Кеш городов пуст, повторная загрузка из Data/cities.json");
                return LoadFromFile(_environment);
            }
        }

        private bool TryGetCached(out HashSet<string> cityNames)
        {
            if (_memoryCache.TryGetValue<HashSet<string>>(CityNamesCacheKeys.Names, out var cached)
                && cached is { Count: > 0 })
            {
                cityNames = cached;
                return true;
            }

            cityNames = [];
            return false;
        }

        private HashSet<string> LoadFromFile(IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "cities.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл городов не найден: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            using var document = JsonDocument.Parse(json);
            var cities = document.RootElement
                .GetProperty("lists")
                .GetProperty("cities")
                .EnumerateArray()
                .Select(c => c.GetProperty("city").GetString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!.ToLowerInvariant())
                .ToList();

            var cityNames = cities.ToHashSet(StringComparer.OrdinalIgnoreCase);
            _memoryCache.Set(CityNamesCacheKeys.Names, cityNames);
            _logger.LogInformation("Загружено {Count} городов", cityNames.Count);
            return cityNames;
        }
    }
}
