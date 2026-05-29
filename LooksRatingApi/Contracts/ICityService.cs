using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Contracts
{
    public interface ICityService
    {
        bool IsCityValid(string cityName);
        bool TryResolveCanonicalCity(string cityInput, out string canonicalCity);
        IReadOnlyList<string> GetAllCities();
    }
}