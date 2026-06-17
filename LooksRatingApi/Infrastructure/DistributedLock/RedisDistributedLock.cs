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

        private const string RenewScript = """
            if redis.call('GET', @key) == @token then
                return redis.call('EXPIRE', @key, @expiry)
            end
            return 0
            """;

        private readonly IDatabase _database;
        private readonly LuaScript _acquireScript;
        private readonly LuaScript _releaseScript;
        private readonly LuaScript _renewScript;

        public RedisDistributedLock(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
            _acquireScript = LuaScript.Prepare(AcquireScript);
            _releaseScript = LuaScript.Prepare(ReleaseScript);
            _renewScript = LuaScript.Prepare(RenewScript);
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
                return null;

            return new RedisDistributedLockHandle(key, token, _database, _releaseScript, _renewScript);
        }

        public async Task<bool> RenewAsync(
            string key,
            string lockToken,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Lock key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(lockToken))
                throw new ArgumentException("Lock token is required.", nameof(lockToken));
            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl));

            var ttlSeconds = Math.Max(1, (int)Math.Ceiling(ttl.TotalSeconds));
            var renewed = (int)await _renewScript.EvaluateAsync(
                _database,
                new
                {
                    key = (RedisKey)key,
                    token = (RedisValue)lockToken,
                    expiry = (RedisValue)ttlSeconds,
                });

            return renewed == 1;
        }

        public async Task<bool> ReleaseAsync(
            string key,
            string lockToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Lock key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(lockToken))
                throw new ArgumentException("Lock token is required.", nameof(lockToken));

            var released = (int)await _releaseScript.EvaluateAsync(
                _database,
                new { key = (RedisKey)key, token = (RedisValue)lockToken });

            return released == 1;
        }
    }

    internal sealed class RedisDistributedLockHandle : IRedisDistributedLockHandle
    {
        private readonly IDatabase _database;
        private readonly LuaScript _releaseScript;
        private readonly LuaScript _renewScript;
        private int _released;

        public RedisDistributedLockHandle(
            string key,
            string token,
            IDatabase database,
            LuaScript releaseScript,
            LuaScript renewScript)
        {
            Key = key;
            Token = token;
            _database = database;
            _releaseScript = releaseScript;
            _renewScript = renewScript;
        }

        public string Key { get; }

        public string Token { get; }

        public Task<bool> RenewAsync(TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _released, 0, 0) != 0)
                return Task.FromResult(false);

            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl));

            var ttlSeconds = Math.Max(1, (int)Math.Ceiling(ttl.TotalSeconds));
            return RenewCoreAsync(ttlSeconds);
        }

        private async Task<bool> RenewCoreAsync(int ttlSeconds)
        {
            var renewed = (int)await _renewScript.EvaluateAsync(
                _database,
                new
                {
                    key = (RedisKey)Key,
                    token = (RedisValue)Token,
                    expiry = (RedisValue)ttlSeconds,
                });

            return renewed == 1;
        }

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
