namespace LooksRatingApi.Services.SparksWallet
{
    /// <summary>
    /// Canonical sparks-to-Telegram-Stars exchange rules for VIP gift shop.
    /// </summary>
    public static class SparksGiftExchangeRules
    {
        public const int SparksPerStar = 12;

        public static readonly int[] AllowedStarTiers = { 100, 200, 300, 400 };

        public static bool IsAllowedStarTier(int starTier) =>
            Array.IndexOf(AllowedStarTiers, starTier) >= 0;

        public static bool TryGetSparksCost(int starTier, out decimal sparksCost)
        {
            if (!IsAllowedStarTier(starTier))
            {
                sparksCost = 0m;
                return false;
            }

            sparksCost = starTier * SparksPerStar;
            return true;
        }

        public static IReadOnlyList<GiftExchangeRate> GetRates() =>
            AllowedStarTiers
                .Select(tier => new GiftExchangeRate(tier, tier * SparksPerStar))
                .ToArray();

        public sealed record GiftExchangeRate(int StarTier, decimal SparksCost);
    }
}
