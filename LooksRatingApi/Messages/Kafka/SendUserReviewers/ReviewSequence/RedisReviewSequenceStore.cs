using StackExchange.Redis;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public sealed class RedisReviewSequenceStore : IReviewSequenceStore
    {
        private static readonly LuaScript ResolveNextScript = LuaScript.Prepare(@"
local current = redis.call('GET', @key)
local prev = nil
if current then prev = tonumber(current) end
local max = tonumber(@max)
local next
if prev == nil or prev < 1 then
    next = 1
elseif prev >= max then
    next = 1
else
    next = prev + 1
end
redis.call('SET', @key, next)
return next
");

        private readonly IDatabase _database;

        public RedisReviewSequenceStore(IDatabase database)
        {
            _database = database;
        }

        public int? GetLastReviewsCount(ReviewSequenceKey key)
        {
            var value = _database.StringGet(ReviewRedisKeys.SequenceCount(key.PhotoProfileId));
            if (value.IsNullOrEmpty || !value.TryParse(out int count))
            {
                return null;
            }

            return count;
        }

        public void SetLastReviewsCount(ReviewSequenceKey key, int reviewsCount)
        {
            _database.StringSet(ReviewRedisKeys.SequenceCount(key.PhotoProfileId), reviewsCount);
        }

        public int ResolveNextReviewsCount(ReviewSequenceKey key, Func<int?, int> calculateNext)
        {
            var redisKey = (RedisKey)ReviewRedisKeys.SequenceCount(key.PhotoProfileId);
            var result = _database.ScriptEvaluate(
                ResolveNextScript,
                new { key = redisKey, max = ReviewSequenceConstants.MaxReviewsCount });

            if (result.IsNull)
            {
                return calculateNext(null);
            }

            return (int)result;
        }
    }
}
