using System;
using System.Collections.Generic;
using System.Linq;
using LooksRatingApi.Contracts;
using LooksRatingApi.DtoModels.ValueObjectDto;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace LooksRatingApi.Services
{
    public class CityService : ICityService
    {
        private readonly IMemoryCache _memoryCache;
        private const string CityNamesCacheKey = "key_cities_names";

        public CityService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public  bool IsCityValid(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                return false;
            }

            if (!_memoryCache.TryGetValue<HashSet<string>>(CityNamesCacheKey, out var cityNames) || cityNames is null)
            {
                return false;
            }

            return cityNames.Contains(NormalizeForLookup(cityName));
        }

        public bool TryResolveCanonicalCity(string cityInput, out string canonicalCity)
        {
            canonicalCity = string.Empty;
            if (string.IsNullOrWhiteSpace(cityInput))
            {
                return false;
            }

            if (!_memoryCache.TryGetValue<HashSet<string>>(CityNamesCacheKey, out var cityNames) || cityNames is null)
            {
                return false;
            }

            var normalized = NormalizeForLookup(cityInput);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (cityNames.Contains(normalized))
            {
                canonicalCity = normalized;
                return true;
            }

            var alternatives = new[]
            {
                normalized.Replace('ё', 'е'),
                normalized.Replace('-', ' '),
                normalized.Replace(' ', '-'),
            };

            foreach (var candidate in alternatives)
            {
                if (cityNames.Contains(candidate))
                {
                    canonicalCity = candidate;
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<string> GetAllCities()
        {
            if (!_memoryCache.TryGetValue<HashSet<string>>(CityNamesCacheKey, out var cityNames) || cityNames is null)
            {
                return Array.Empty<string>();
            }

            return cityNames.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string NormalizeForLookup(string city)
        {
            var normalized = city.Trim().ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"^г\.\s*", string.Empty);
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized;
        }
    }
}