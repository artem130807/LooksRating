using StackExchange.Redis;

namespace LooksRatingApi.Infrastructure.DistributedLock
{
    public sealed class RedisDistributedLock : IRedisDistributedLock
    {
        private const string AcquireScript = """
            return redis.call('SET', @key, @token, 'NX', 'EX', @expiry) and 1 or 0
            """;

        private const string ReleaseScript = """
            if redis.call('GET', @key) == @token then
                return redis.call('DEL', @key)
            end
            return 0
            """;

        private readonly IDatabase _database;
        private readonly LuaScript _acquireScript;
        private readonly LuaScript _releaseScript;
        private readonly ILogger<RedisDistributedLock> _logger;

        public RedisDistributedLock(
            IConnectionMultiplexer redis,
            ILogger<RedisDistributedLock> logger)
        {
            _database = redis.GetDatabase();
            _acquireScript = LuaScript.Prepare(AcquireScript);
            _releaseScript = LuaScript.Prepare(ReleaseScript);
            _logger = logger;
        }

        public Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken = default) =>
            _database.KeyExistsAsync(key);

        public async Task<IRedisDistributedLockHandle?> TryAcquireAsync(
            string key,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl));

            var token = Guid.NewGuid().ToString("N");
            var ttlSeconds = Math.Max(1, (int)Math.Ceiling(ttl.TotalSeconds));

            var acquired = (int)await _acquireScript.EvaluateAsync(
                _database,
                new
                {
                    key = (RedisKey)key,
                    token = (RedisValue)token,
                    expiry = (RedisValue)ttlSeconds,
                });

            if (acquired != 1)
            {
                _logger.LogDebug("Redis lock занят: {Key}", key);
                return null;
            }

            _logger.LogDebug("Redis lock получен: {Key}, ttl={TtlSeconds}s", key, ttlSeconds);
            return new RedisDistributedLockHandle(key, token, _database, _releaseScript);
        }
    }

    internal sealed class RedisDistributedLockHandle : IRedisDistributedLockHandle
    {
        private readonly IDatabase _database;
        private readonly LuaScript _releaseScript;
        private int _released;

        public RedisDistributedLockHandle(
            string key,
            string token,
            IDatabase database,
            LuaScript releaseScript)
        {
            Key = key;
            Token = token;
            _database = database;
            _releaseScript = releaseScript;
        }

        public string Key { get; }

        internal string Token { get; }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
                return;

            await _releaseScript.EvaluateAsync(
                _database,
                new { key = (RedisKey)Key, token = (RedisValue)Token });
        }
    }
}
