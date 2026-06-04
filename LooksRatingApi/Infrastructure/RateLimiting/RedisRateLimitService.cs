using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RedisRateLimitService : IRateLimitService
    {
        private const string AcquireScript = """
            local current = tonumber(redis.call('INCR', KEYS[1]))
            if current == 1 then
                redis.call('EXPIRE', KEYS[1], ARGV[1])
            end
            local limit = tonumber(ARGV[2])
            if current > limit then
                redis.call('DECR', KEYS[1])
                local ttl = redis.call('TTL', KEYS[1])
                if ttl < 0 then
                    ttl = tonumber(ARGV[1])
                end
                return { 0, ttl }
            end
            return { 1, 0 }
            """;

        private readonly IDatabase _database;
        private readonly RateLimitingOptions _options;
        private readonly ILogger<RedisRateLimitService> _logger;
        private readonly LuaScript _acquireScript;

        public RedisRateLimitService(
            IDatabase database,
            IOptions<RateLimitingOptions> options,
            ILogger<RedisRateLimitService> logger)
        {
            _database = database;
            _options = options.Value;
            _logger = logger;
            _acquireScript = LuaScript.Prepare(AcquireScript);
        }

        public async Task<RateLimitAcquireResult> TryAcquireAsync(
            string policyName,
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Policies.TryGetValue(policyName, out var policy))
            {
                _logger.LogError("Rate limit policy {PolicyName} is not configured", policyName);
                return _options.FailOpen
                    ? RateLimitAcquireResult.Allowed
                    : new RateLimitAcquireResult(false, 60);
            }

            if (policy.BurstPermitLimit is > 0 && policy.BurstWindowSeconds is > 0)
            {
                var burstResult = await TryAcquireWindowAsync(
                    policyName,
                    partitionKey,
                    policy.BurstPermitLimit.Value,
                    policy.BurstWindowSeconds.Value,
                    "burst",
                    cancellationToken);

                if (!burstResult.IsAcquired)
                {
                    return burstResult;
                }
            }

            return await TryAcquireWindowAsync(
                policyName,
                partitionKey,
                policy.PermitLimit,
                policy.WindowSeconds,
                "window",
                cancellationToken);
        }

        private async Task<RateLimitAcquireResult> TryAcquireWindowAsync(
            string policyName,
            string partitionKey,
            int permitLimit,
            int windowSeconds,
            string windowKind,
            CancellationToken cancellationToken)
        {
            var windowId = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / windowSeconds;
            var redisKey = BuildRedisKey(policyName, windowKind, partitionKey, windowId);
            var expirySeconds = windowSeconds + 1;

            try
            {
                var result = (RedisResult[]?)await _database.ScriptEvaluateAsync(
                    _acquireScript,
                    new { key = (RedisKey)redisKey, expiry = (RedisValue)expirySeconds, limit = (RedisValue)permitLimit });

                if (result is null || result.Length < 2)
                {
                    return RateLimitAcquireResult.Allowed;
                }

                var allowed = (int)result[0] == 1;
                var retryAfter = (int)result[1];
                return new RateLimitAcquireResult(allowed, retryAfter > 0 ? retryAfter : windowSeconds);
            }
            catch (Exception ex)
            {
                if (_options.FailOpen)
                {
                    _logger.LogWarning(
                        ex,
                        "Redis rate limit check failed for policy {PolicyName}, allowing request (FailOpen)",
                        policyName);
                    return RateLimitAcquireResult.Allowed;
                }

                _logger.LogError(
                    ex,
                    "Redis rate limit check failed for policy {PolicyName}, rejecting request",
                    policyName);
                return new RateLimitAcquireResult(false, windowSeconds);
            }
        }

        private string BuildRedisKey(string policyName, string windowKind, string partitionKey, long windowId)
        {
            return $"{_options.KeyPrefix}:{policyName}:{windowKind}:{partitionKey}:{windowId}";
        }
    }
}
