using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace LooksRatingApi.Services
{
    public class BadWordService:IBadWordService
    {
        private readonly IMemoryCache _memoryCache;
        private const string BadWordCacheKey = "key_bad_word";
        public BadWordService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public bool IsTextValid(string text)
        {
             if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (!_memoryCache.TryGetValue<HashSet<string>>(BadWordCacheKey, out var textWord) || textWord is null)
            {
                return false;
            }

            return textWord.Contains(text.ToLowerInvariant());
        }
    }
}