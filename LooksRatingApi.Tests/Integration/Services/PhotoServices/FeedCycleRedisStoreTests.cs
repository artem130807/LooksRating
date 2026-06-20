using LooksRatingApi.Services;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Integration.Services.PhotoServices;

[Collection(IntegrationCollection.Name)]
public sealed class FeedCycleRedisStoreTests
{
    private readonly RedisFixture _redis;

    public FeedCycleRedisStoreTests(RedisFixture redis)
    {
        _redis = redis;
    }

    [SkippableFact]
    public async Task GetRatedProfileIdsAsync_ReturnsMembersFromRedisSet()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var db = _redis.Connection.GetDatabase();
        var reviewerId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var profileA = Guid.NewGuid();
        var profileB = Guid.NewGuid();
        var ratedKey = PhotoRedisKeys.UserRatedSet(reviewerId, seasonId);

        await db.KeyDeleteAsync(ratedKey);
        await db.SetAddAsync(ratedKey, profileA.ToString());
        await db.SetAddAsync(ratedKey, profileB.ToString());

        var store = new FeedCycleRedisStore(_redis.Connection);
        var rated = await store.GetRatedProfileIdsAsync(reviewerId, seasonId);

        rated.Should().BeEquivalentTo(new[] { profileA, profileB });
    }

    [SkippableFact]
    public async Task ResetCycleAsync_DeletesRatedSetAndUpdatesAnchor()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var db = _redis.Connection.GetDatabase();
        var reviewerId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var ratedKey = PhotoRedisKeys.UserRatedSet(reviewerId, seasonId);
        var anchorKey = PhotoRedisKeys.CycleAnchor(reviewerId, seasonId);
        var resetAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        await db.SetAddAsync(ratedKey, Guid.NewGuid().ToString());
        await db.StringSetAsync(anchorKey, DateTime.UtcNow.Ticks.ToString());

        var store = new FeedCycleRedisStore(_redis.Connection);
        await store.ResetCycleAsync(reviewerId, seasonId, resetAt);

        (await db.KeyExistsAsync(ratedKey)).Should().BeFalse();
        var anchorValue = await db.StringGetAsync(anchorKey);
        anchorValue.HasValue.Should().BeTrue();
        new DateTime(long.Parse(anchorValue.ToString()!), DateTimeKind.Utc).Should().Be(resetAt);
    }

    [SkippableFact]
    public async Task GetFeedRatingCounterAsync_ReturnsZeroWhenMissing()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var reviewerId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var counterKey = PhotoRedisKeys.FeedRatingCounter(reviewerId, seasonId);
        await _redis.Connection.GetDatabase().KeyDeleteAsync(counterKey);

        var store = new FeedCycleRedisStore(_redis.Connection);
        var count = await store.GetFeedRatingCounterAsync(reviewerId, seasonId);

        count.Should().Be(0);
    }
}
