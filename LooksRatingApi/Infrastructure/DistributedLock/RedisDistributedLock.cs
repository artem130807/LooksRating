using StackExchange.Redis;

namespace LooksRatingApi.Infrastructure.DistributedLock
{
    public sealed class RedisDistributedLock : IRedisDistributedLock
    {
        private const string AcquireScript = """
            return redis.call('SET', KEYS[1], ARGV[1], 'NX', 'EX', ARGV[2]) and 1 or 0
            """;

        private const string ReleaseScript = """
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            end
            return 0
            """;

        private readonly IDatabase _database;
        private readonly LuaScript _acquireScript;
        private readonly LuaScript _releaseScript;

        public RedisDistributedLock(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
            _acquireScript = LuaScript.Prepare(AcquireScript);
            _releaseScript = LuaScript.Prepare(ReleaseScript);
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

            var acquired = (int)await _database.ScriptEvaluateAsync(
                _acquireScript,
                new
                {
                    key = (RedisKey)key,
                    token = (RedisValue)token,
                    expiry = (RedisValue)ttlSeconds
                });

            if (acquired != 1)
                return null;

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

            await _database.ScriptEvaluateAsync(
                _releaseScript,
                new { key = (RedisKey)Key, token = (RedisValue)Token });
        }
    }
}
