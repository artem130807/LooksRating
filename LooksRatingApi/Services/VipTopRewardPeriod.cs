using LooksRatingApi.Constants;

namespace LooksRatingApi.Services
{
    internal static class VipTopRewardPeriod
    {
        /// <param name="applicationLocalNow">Local time in the application timezone (Quartz: Europe/Moscow).</param>
        public static string BuildKey(Guid seasonId, DateTime applicationLocalNow)
        {
            var days = VipTopRewardSchedule.GetDaysSinceEpoch(applicationLocalNow);
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

        public static string BuildSparksPayload(
            string periodKey,
            int place,
            long telegramId,
            string categoryFingerprint)
        {
            var payload = $"vip-sparks:{periodKey}:{place}:{telegramId}:{categoryFingerprint}";
            if (payload.Length > VipTopConstants.ExtensionPayloadMaxLength)
            {
                throw new InvalidOperationException($"VIP sparks payload exceeds {VipTopConstants.ExtensionPayloadMaxLength} characters.");
            }

            return payload;
        }
    }
}
