using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public sealed class TheBestWeekLockService
    {
        private const string LockKey = "thebestweek:refresh_lock";
        private readonly IDatabase _redis;

        public TheBestWeekLockService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public Task<bool> IsRefreshInProgressAsync() => _redis.KeyExistsAsync(LockKey);

        public Task StartRefreshAsync(TimeSpan ttl) =>
            _redis.StringSetAsync(LockKey, "locked", ttl);

        public Task EndRefreshAsync() => _redis.KeyDeleteAsync(LockKey);
    }
}
