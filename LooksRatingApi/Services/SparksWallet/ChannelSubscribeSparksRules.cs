namespace LooksRatingApi.Services.SparksWallet
{
    public static class ChannelSubscribeSparksRules
    {
        public const decimal RewardSparks = 50m;
        public const int RewardProductCode = VipTopRules.VipProductCode;

        public static string BuildPayload(long telegramId) => $"channel-subscribe:{telegramId}";
    }
}
