using LooksRatingApi.Contracts;
using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.Services
{
    internal static class SeasonTopPlacement
    {
        public static IReadOnlyList<SeasonTopSparksRewardRecipient> GetSparksRewardRecipients(
            IReadOnlyList<VipTopCategory> categories)
        {
            var recipients = new List<SeasonTopSparksRewardRecipient>();

            foreach (var category in categories)
            {
                var categoryFingerprint = VipTopPlacement.BuildCategoryFingerprint(category);
                var ranked = category.RankedProfiles;
                var lastPlaceIndex = Math.Min(SeasonTopRules.GiftPlaceTo, ranked.Count);

                for (var place = SeasonTopRules.GiftPlaceFrom; place <= lastPlaceIndex; place++)
                {
                    var profile = ranked[place - 1];
                    if (profile.TelegramId <= 0)
                    {
                        continue;
                    }

                    var sparksAmount = SeasonTopSparksRewards.GetSparksForPlace(place);
                    if (sparksAmount <= 0)
                    {
                        continue;
                    }

                    recipients.Add(new SeasonTopSparksRewardRecipient(
                        profile.TelegramId,
                        place,
                        sparksAmount,
                        categoryFingerprint));
                }
            }

            return recipients;
        }
    }

    internal sealed record SeasonTopSparksRewardRecipient(
        long TelegramId,
        int Place,
        decimal SparksAmount,
        string CategoryFingerprint);
}
