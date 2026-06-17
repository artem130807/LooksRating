using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Integration.ReviewSequence;

[Collection(IntegrationCollection.Name)]
public sealed class RedisReviewSequenceStoreTests
{
    private readonly RedisFixture _redis;

    public RedisReviewSequenceStoreTests(RedisFixture redis)
    {
        _redis = redis;
    }

    [Fact]
    public void ResolveNextReviewsCount_AdvancesCycleAtomically()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var database = _redis.Connection.GetDatabase();
        var store = new RedisReviewSequenceStore(database);
        var calculator = new ReviewSequenceCalculator();
        var key = new ReviewSequenceKey(Guid.NewGuid());
        var redisKey = (RedisKey)ReviewRedisKeys.SequenceCount(key.PhotoProfileId);

        try
        {
            for (var expected = 1; expected <= 10; expected++)
            {
                var next = store.ResolveNextReviewsCount(key, calculator.CalculateNextReviewsCount);
                next.Should().Be(expected);
            }

            store.ResolveNextReviewsCount(key, calculator.CalculateNextReviewsCount).Should().Be(1);
        }
        finally
        {
            database.KeyDelete(redisKey);
        }
    }

    [Fact]
    public void SetLastReviewsCount_IsVisibleAcrossStoreInstances()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var database = _redis.Connection.GetDatabase();
        var storeA = new RedisReviewSequenceStore(database);
        var storeB = new RedisReviewSequenceStore(database);
        var key = new ReviewSequenceKey(Guid.NewGuid());
        var redisKey = (RedisKey)ReviewRedisKeys.SequenceCount(key.PhotoProfileId);

        try
        {
            storeA.SetLastReviewsCount(key, 7);

            storeB.GetLastReviewsCount(key).Should().Be(7);
        }
        finally
        {
            database.KeyDelete(redisKey);
        }
    }
}
