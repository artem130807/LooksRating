namespace LooksRatingApi.Infrastructure.DistributedLock
{
    public static class DistributedLockKeys
    {
        public const string Archive = "looksrating:lock:archive";
        public const string TheBestWeekRefresh = "looksrating:lock:thebestweek";
        public const string VipStatusExpiry = "looksrating:lock:vip-expiry";
        public const string VipTopSparksReward = "looksrating:lock:vip-top-sparks-reward";

        public static string ChannelSubscribeBonus(long telegramId) =>
            $"looksrating:lock:channel-subscribe:{telegramId}";

        public static string UserTicketPhotoProfile(Guid photoProfileId) =>
            $"looksrating:lock:user-ticket:photo-profile:{photoProfileId:N}";
    }
}
