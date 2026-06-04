using LooksRatingApi.Constants;

namespace LooksRatingApi.Services
{
    internal static class VipTopRewardPeriod
    {
        private static readonly DateTime EpochUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static string BuildKey(Guid seasonId, DateTime utcNow)
        {
            var days = (int)(utcNow.Date - EpochUtc).TotalDays;
            var period = Math.Max(0, days / VipTopRules.RewardPeriodDays);
            return $"{seasonId:N}:{period}";
        }

        public static string BuildExtensionPayload(string periodKey, long telegramId)
        {
            var payload = $"vip-top-ext:{periodKey}:{telegramId}";
            if (payload.Length > VipTopConstants.ExtensionPayloadMaxLength)
            {
                throw new InvalidOperationException($"VIP extension payload exceeds {VipTopConstants.ExtensionPayloadMaxLength} characters.");
            }

            return payload;
        }
    }
}
