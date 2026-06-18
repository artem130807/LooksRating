namespace LooksRatingApi.CQRS.Payments.Query.GetGiftExchangeRates
{
    public sealed class GiftExchangeRatesResponse
    {
        public int SparksPerStar { get; init; }

        public IReadOnlyList<GiftExchangeRateItem> Gifts { get; init; } = Array.Empty<GiftExchangeRateItem>();
    }

    public sealed class GiftExchangeRateItem
    {
        public int StarTier { get; init; }

        public decimal SparksCost { get; init; }
    }
}
