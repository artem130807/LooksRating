namespace LooksRatingApi.Services
{
    public static class PhotoRankingScore
    {
        public const decimal PriorMean = 8.0m;
        public const int PriorWeight = 5;
        public const decimal UnratedScore = -1m;

        public static double ToSortScore(decimal rating, int ratingCount) =>
            (double)ToRankScore(rating, ratingCount);

        public static decimal ToRankScore(decimal rating, int ratingCount)
        {
            var votes = Math.Max(0, ratingCount);
            if (votes == 0)
            {
                return UnratedScore;
            }

            return ToBayesianScore(rating, votes);
        }

        public static decimal ToBayesianScore(decimal rating, int ratingCount)
        {
            var votes = Math.Max(0, ratingCount);
            var weight = (decimal)PriorWeight;
            return ((rating * votes) + (PriorMean * weight)) / (votes + weight);
        }

        public static int Compare(decimal ratingA, int ratingCountA, decimal ratingB, int ratingCountB)
        {
            var hasVotesA = ratingCountA > 0 ? 1 : 0;
            var hasVotesB = ratingCountB > 0 ? 1 : 0;
            var votesCompare = hasVotesB.CompareTo(hasVotesA);
            if (votesCompare != 0)
            {
                return votesCompare;
            }

            var scoreCompare = ToRankScore(ratingB, ratingCountB)
                .CompareTo(ToRankScore(ratingA, ratingCountA));
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            var ratingCompare = ratingB.CompareTo(ratingA);
            if (ratingCompare != 0)
            {
                return ratingCompare;
            }

            return ratingCountB.CompareTo(ratingCountA);
        }

        public static int Compare(
            decimal ratingA,
            int ratingCountA,
            DateTime createdAtA,
            decimal ratingB,
            int ratingCountB,
            DateTime createdAtB)
        {
            var rankingCompare = Compare(ratingA, ratingCountA, ratingB, ratingCountB);
            if (rankingCompare != 0)
            {
                return rankingCompare;
            }

            return createdAtB.CompareTo(createdAtA);
        }

        public static int Compare(
            decimal ratingA,
            int ratingCountA,
            DateTime createdAtA,
            Guid idA,
            decimal ratingB,
            int ratingCountB,
            DateTime createdAtB,
            Guid idB)
        {
            var rankingCompare = Compare(ratingA, ratingCountA, createdAtA, ratingB, ratingCountB, createdAtB);
            if (rankingCompare != 0)
            {
                return rankingCompare;
            }

            return idA.CompareTo(idB);
        }
    }
}
