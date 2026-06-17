using LooksRatingApi.Constants;

namespace LooksRatingApi.Services
{
    internal static class SeasonTopRewardPeriod
    {
        public static string BuildKey(Guid seasonId) => $"{seasonId:N}:close";

        public static string BuildSparksPayload(
            string periodKey,
            int place,
            long telegramId,
            string categoryFingerprint)
        {
            var payload = $"season-sparks:{periodKey}:{place}:{telegramId}:{categoryFingerprint}";
            if (payload.Length > VipTopConstants.ExtensionPayloadMaxLength)
            {
                throw new InvalidOperationException(
                    $"Season sparks payload exceeds {VipTopConstants.ExtensionPayloadMaxLength} characters.");
            }

            return payload;
        }
    }
}
