using LooksRatingApi.Contracts;

namespace LooksRatingApi.Services
{
    internal static class VipTopPlacement
    {
        public static IReadOnlyList<long> GetExtensionTelegramIds(IReadOnlyList<VipTopCategory> categories)
        {
            var telegramIds = new HashSet<long>();

            foreach (var category in categories)
            {
                var ranked = category.RankedProfiles;
                var lastIndex = Math.Min(VipTopRules.ExtensionPlaceTo, ranked.Count);
                for (var placeIndex = VipTopRules.ExtensionPlaceFrom - 1; placeIndex < lastIndex; placeIndex++)
                {
                    var telegramId = ranked[placeIndex].TelegramId;
                    if (telegramId > 0)
                    {
                        telegramIds.Add(telegramId);
                    }
                }
            }

            return telegramIds.ToList();
        }
    }
}
