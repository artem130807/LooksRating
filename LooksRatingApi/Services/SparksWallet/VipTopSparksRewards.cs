using LooksRatingApi.Services;

namespace LooksRatingApi.Services.SparksWallet
{
    internal static class VipTopSparksRewards
    {
        private static readonly decimal[] PlaceSparksAmounts =
        {
            4000m, // 1 — 400★
            3000m, // 2 — 300★
            2000m, // 3 — 200★
            2000m, // 4 — 200★
            2000m, // 5 — 200★
        };

        public static decimal GetSparksForPlace(int place)
        {
            if (place < VipTopRules.GiftPlaceFrom || place > VipTopRules.GiftPlaceTo)
            {
                return 0m;
            }

            return PlaceSparksAmounts[place - VipTopRules.GiftPlaceFrom];
        }
    }
}
