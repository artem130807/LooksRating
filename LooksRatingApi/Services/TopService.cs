using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Services
{
    public class TopService
    {
        private static List<int[]> intsList = new List<int[]>()
        {
            new int[] {11, 12, 13},
            new int[] {14, 15, 16},
            new int[] {17, 18, 19},
            new int[] {20, 21, 22},
            new int[] {23, 24, 25},
            new int[] {26, 27, 28},
            new int[] {28, 30, 31},
            new int[] {32, 33, 34},
            new int[] {35, 36, 37},
            new int[] {38, 39, 40},
            new int[] {41, 42, 43},
            new int[] {44, 45, 46},
        };
        public static int[] GetTop(int age)
        {
            foreach (var ints in intsList)
            {
                if (ints.Contains(age))
                {
                    return ints;
                }
            }
            return Array.Empty<int>();
        }
        public static List<int[]> GetIntsList()
        {
            return intsList;
        }
        public static bool MatchesAge(int viewerAge, int photoAge)
        {
            var topAge = GetTop(viewerAge);
            if (topAge.Length == 0)
            {
                return false;
            }

            return photoAge == topAge[0]
                || photoAge == topAge[1]
                || photoAge == topAge[2];
        }
    }
}