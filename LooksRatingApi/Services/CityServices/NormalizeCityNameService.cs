using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;

namespace LooksRatingApi.Services.CityServices
{
    public class NormalizeCityNameService : INormalizeCityNameService
    {
        public string Normalize(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return "unknown";
    
            var normalized = city.Trim().ToLowerInvariant();
            
            normalized = Regex.Replace(normalized, @"[\s-]+", "_");
            
            normalized = Regex.Replace(normalized, @"[^a-zа-яё0-9_]", "");
            
            normalized = Transliterate(normalized);
            
            return normalized;
        }
        private string Transliterate(string text)
        {
            var map = new Dictionary<char, string>
            {
                ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
                ['е'] = "e", ['ё'] = "yo", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
                ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
                ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
                ['у'] = "u", ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch",
                ['ш'] = "sh", ['щ'] = "shch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "",
                ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
            };
            var result = new StringBuilder();
            foreach (char c in text)
            {
                if (map.TryGetValue(c, out string? latin))
                    result.Append(latin);
                else
                    result.Append(c);
            }
            return result.ToString();
        }
    }
}