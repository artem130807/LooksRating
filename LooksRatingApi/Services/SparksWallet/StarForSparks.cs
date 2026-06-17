using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Services.SparksWallet
{
    public static class StarForSparks
    {
        private static readonly Dictionary<int, decimal> price = new Dictionary<int, decimal>
        {
            {100, 1000},
            {200, 2000},
            {300, 3000},
            {400, 4000}
        };
        public static decimal WritingOfSparks(int key)
        {
            var sparks = price.GetValueOrDefault(key);
            return sparks;
        }
    }
}