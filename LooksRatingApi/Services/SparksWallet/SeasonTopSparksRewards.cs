using LooksRatingApi.Services;

namespace LooksRatingApi.Services.SparksWallet
{
    internal static class SeasonTopSparksRewards
    {
        private static readonly decimal[] PlaceSparksAmounts =
        {
            800m, // 1 — 80★
            600m, // 2 — 60★
            500m, // 3 — 50★
            400m, // 4 — 40★
            400m, // 5 — 40★
            300m, // 6 — 30★
            300m, // 7 — 30★
            200m, // 8 — 20★
            200m, // 9 — 20★
            200m, // 10 — 20★
        };

        public static decimal GetSparksForPlace(int place)
        {
            if (place < SeasonTopRules.GiftPlaceFrom || place > SeasonTopRules.GiftPlaceTo)
            {
                return 0m;
            }

            return PlaceSparksAmounts[place - SeasonTopRules.GiftPlaceFrom];
        }
    }
}
