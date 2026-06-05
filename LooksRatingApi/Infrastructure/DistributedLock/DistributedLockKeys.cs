namespace LooksRatingApi.Infrastructure.DistributedLock
{
    public static class DistributedLockKeys
    {
        public const string Archive = "looksrating:lock:archive";
        public const string TheBestWeekRefresh = "looksrating:lock:thebestweek";
        public const string VipStatusExpiry = "looksrating:lock:vip-expiry";
    }
}
