namespace LooksRatingApi.Services
{
    public class TopService
    {
        public const int AllAges = 0;
        public const int MinBracketAge = 14;
        public const int MaxBracketAge = 46;

        private static readonly int[][] AgeBrackets =
        {
            new[] { 14, 15, 16 },
            new[] { 17, 18, 19 },
            new[] { 20, 21, 22 },
            new[] { 23, 24, 25 },
            new[] { 26, 27, 28 },
            new[] { 28, 30, 31 },
            new[] { 32, 33, 34 },
            new[] { 35, 36, 37 },
            new[] { 38, 39, 40 },
            new[] { 41, 42, 43 },
            new[] { 44, 45, 46 },
        };

        public static int[] GetTop(int age)
        {
            if (age == AllAges)
            {
                return Array.Empty<int>();
            }

            foreach (var bracket in AgeBrackets)
            {
                if (bracket.Contains(age))
                {
                    return bracket;
                }
            }

            return Array.Empty<int>();
        }

        public static IReadOnlyList<int[]> GetIntsList() => AgeBrackets;

        public static bool IsValidBracketAge(int age) => GetTop(age).Length > 0;

        public static bool IsValidFeedAge(int age) => age == AllAges || IsValidBracketAge(age);

        public static bool IsValidNominationAge(int age) => IsValidBracketAge(age);

        public static bool MatchesAge(int viewerAge, int photoAge)
        {
            if (viewerAge == AllAges)
            {
                return true;
            }

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
