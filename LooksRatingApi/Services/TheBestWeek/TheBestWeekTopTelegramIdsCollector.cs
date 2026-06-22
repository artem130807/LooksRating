using LooksRatingApi.Enums;
using LooksRatingApi.Services;

namespace LooksRatingApi.Services.TheBestWeek
{
    public static class TheBestWeekTopTelegramIdsCollector
    {
        public const int TopPerCategory = 10;

        public static HashSet<long> CollectForCity(string city, IReadOnlyList<TheBestWeekSnapshotItem> snapshotItems)
        {
            var ids = new HashSet<long>();
            if (string.IsNullOrWhiteSpace(city) || snapshotItems.Count == 0)
            {
                return ids;
            }

            var profilesM = snapshotItems
                .Where(x => x.GenderNomination == GenderEnum.Male)
                .ToList();
            var profilesG = snapshotItems
                .Where(x => x.GenderNomination == GenderEnum.Female)
                .ToList();

            foreach (var ageBracket in TopService.GetIntsList())
            {
                foreach (var telegramId in SelectTopTelegramIds(profilesM, city, ageBracket))
                {
                    ids.Add(telegramId);
                }

                foreach (var telegramId in SelectTopTelegramIds(profilesG, city, ageBracket))
                {
                    ids.Add(telegramId);
                }
            }

            return ids;
        }

        public static HashSet<long> CollectForWeekRecords(IEnumerable<TheBestWeekWeekRecord> weekRecords)
        {
            var ids = new HashSet<long>();
            foreach (var record in weekRecords)
            {
                foreach (var telegramId in CollectForCity(record.City, record.SnapshotItems))
                {
                    ids.Add(telegramId);
                }
            }

            return ids;
        }

        private static IEnumerable<long> SelectTopTelegramIds(
            IReadOnlyList<TheBestWeekSnapshotItem> profiles,
            string city,
            int[] ageBracket)
        {
            return profiles
                .Where(p => string.Equals(p.City, city, StringComparison.OrdinalIgnoreCase)
                    && ageBracket.Contains(p.AgeNomination))
                .OrderByDescending(p => p.RatingCount > 0 ? 1 : 0)
                .ThenByDescending(p => PhotoRankingScore.ToRankScore(p.Rating, p.RatingCount))
                .ThenByDescending(p => p.Rating)
                .ThenByDescending(p => p.RatingCount)
                .ThenByDescending(p => p.CreatedAt)
                .Take(TopPerCategory)
                .Select(p => p.TelegramId)
                .Where(x => x > 0);
        }
    }
}
