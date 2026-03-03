using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace LooksRatingApi.Services
{
    public class LoadingBadWordService : ILoadingBadWordService
    {
        private readonly IMemoryCache _memoryCache;
        public LoadingBadWordService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public HashSet<string> CreateBadWord(IWebHostEnvironment env)
        {
            var cacheKey = "key_bad_word";
            var filePath = Path.Combine(env.ContentRootPath, "Data", "bad_word.json");
            var json = File.ReadAllText(filePath);
            
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            
            var words = new List<string>();
            var categories = new[] { "russian", "english", "ukrainian", "variations" };
            
            foreach (var category in categories)
            {
                var categoryWords = root.GetProperty(category)
                    .EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(word => !string.IsNullOrEmpty(word))
                    .Select(word => word!.ToLowerInvariant());
                
                words.AddRange(categoryWords);
            }
            
            var badWords = new HashSet<string>(words);
            Console.WriteLine($"Загружено {badWords.Count} нецензурных слов"); 
            _memoryCache.Set(cacheKey, badWords);
            
            return badWords;
        }
    }
}