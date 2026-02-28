using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace LooksRatingApi.Services
{
    public class LoadingCityService:ILoadingCityService
    {
        private readonly IMemoryCache _memoryCache;
        public LoadingCityService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public HashSet<string> CreateCityNames(IWebHostEnvironment env)
        {
            HashSet<string> cityNames; 
            var cacheKey  = "key_cities_names";
            var filePath = Path.Combine(env.ContentRootPath, "Data", "cities.json");
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
            cityNames = [.. cities];
            Console.WriteLine($"Загружено {cityNames.Count} городов"); 
            _memoryCache.Set(cacheKey, cityNames);
            return cityNames;
        }
    }
}