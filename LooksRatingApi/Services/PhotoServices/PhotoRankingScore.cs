namespace LooksRatingApi.Services
{
    public static class PhotoRankingScore
    {
        private const double RatingCountMultiplier = 1_000_000d;

        public static double ToSortScore(decimal rating, int ratingCount) =>
            (double)rating * RatingCountMultiplier + ratingCount;

        public static int Compare(decimal ratingA, int ratingCountA, decimal ratingB, int ratingCountB)
        {
            var ratingCompare = ratingB.CompareTo(ratingA);
            if (ratingCompare != 0)
            {
                return ratingCompare;
            }

            return ratingCountB.CompareTo(ratingCountA);
        }
    }
}
