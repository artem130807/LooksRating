using System.Security.Cryptography;
using System.Text;
using LooksRatingApi.Constants;
using LooksRatingApi.Contracts;
using LooksRatingApi.Services.SparksWallet;

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

        public static IReadOnlyList<VipTopSparksRewardRecipient> GetSparksRewardRecipients(
            IReadOnlyList<VipTopCategory> categories)
        {
            var recipients = new List<VipTopSparksRewardRecipient>();

            foreach (var category in categories)
            {
                var categoryFingerprint = BuildCategoryFingerprint(category);
                var ranked = category.RankedProfiles;
                var lastPlaceIndex = Math.Min(VipTopRules.GiftPlaceTo, ranked.Count);

                for (var place = VipTopRules.GiftPlaceFrom; place <= lastPlaceIndex; place++)
                {
                    var profile = ranked[place - 1];
                    if (profile.TelegramId <= 0)
                    {
                        continue;
                    }

                    var sparksAmount = VipTopSparksRewards.GetSparksForPlace(place);
                    if (sparksAmount <= 0)
                    {
                        continue;
                    }

                    recipients.Add(new VipTopSparksRewardRecipient(
                        profile.TelegramId,
                        place,
                        sparksAmount,
                        categoryFingerprint));
                }
            }

            return recipients;
        }

        internal static string BuildCategoryFingerprint(VipTopCategory category)
        {
            var raw = $"{category.City.Trim().ToLowerInvariant()}|{(int)category.Gender}|{category.AgeBracket}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..8].ToLowerInvariant();
        }
    }
}
