using LooksRatingApi.Services;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Unit.Services.SeasonLifecycle;

[Collection(IntegrationCollection.Name)]
public sealed class RedisSeasonRolloverNotificationStoreTests
{
    private readonly RedisFixture _redis;

    public RedisSeasonRolloverNotificationStoreTests(RedisFixture redis)
    {
        _redis = redis;
    }

    private async Task ResetStoreAsync()
    {
        var connection = _redis.Connection;
        var db = connection.GetDatabase();
        foreach (var endpoint in connection.GetEndPoints())
        {
            var server = connection.GetServer(endpoint);
            if (!server.IsConnected || server.IsReplica)
            {
                continue;
            }

            foreach (var key in server.Keys(pattern: "season-rollover:*", pageSize: 250))
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }

    [SkippableFact]
    public async Task Enqueue_AddsMetaAndPendingIds()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        await ResetStoreAsync();
        var (closedId, newId, eventId) = CreateEventIds();
        var store = CreateStore();

        var enqueued = await store.TryEnqueueBatchAsync(
            CreateRequest(closedId, newId, recipientIds: [1001, 1002]),
            TimeSpan.FromDays(45));

        enqueued.Should().Be(2);
        var db = _redis.Connection.GetDatabase();
        (await db.KeyExistsAsync(PhotoRedisKeys.SeasonRolloverEventMeta(eventId))).Should().BeTrue();
        (await db.SetLengthAsync(PhotoRedisKeys.SeasonRolloverEventPending(eventId))).Should().Be(2);
        (await db.SetContainsAsync(PhotoRedisKeys.SeasonRolloverActiveEvents(), eventId)).Should().BeTrue();
    }

    [SkippableFact]
    public async Task Enqueue_ActiveEventsSet_HasNoExpiry()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        await ResetStoreAsync();
        var (closedId, newId, _) = CreateEventIds();
        var store = CreateStore();

        await store.TryEnqueueBatchAsync(
            CreateRequest(closedId, newId, recipientIds: [1101]),
            TimeSpan.FromDays(45));

        var db = _redis.Connection.GetDatabase();
        var activeTtl = await db.KeyTimeToLiveAsync(PhotoRedisKeys.SeasonRolloverActiveEvents());
        activeTtl.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetPending_ReturnsBatchWithoutRemoving()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        await ResetStoreAsync();
        var (closedId, newId, eventId) = CreateEventIds();
        var store = CreateStore();
        await store.TryEnqueueBatchAsync(
            CreateRequest(closedId, newId, recipientIds: [2001, 2002, 2003]),
            TimeSpan.FromDays(45));

        var pending = await store.GetPendingBatchesAsync(limit: 2);

        pending.Should().ContainSingle();
        pending[0].EventId.Should().Be(eventId);
        pending[0].RecipientTelegramIds.Should().BeEquivalentTo(new long[] { 2001, 2002 });
        var db = _redis.Connection.GetDatabase();
        (await db.SetLengthAsync(PhotoRedisKeys.SeasonRolloverEventPending(pending[0].EventId))).Should().Be(3);
    }

    [SkippableFact]
    public async Task Ack_RemovesDeliveredIds()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        await ResetStoreAsync();
        var (closedId, newId, _) = CreateEventIds();
        var store = CreateStore();
        await store.TryEnqueueBatchAsync(
            CreateRequest(closedId, newId, recipientIds: [3001, 3002, 3003]),
            TimeSpan.FromDays(45));
        var pending = await store.GetPendingBatchesAsync(limit: 10);
        var eventId = pending[0].EventId;

        await store.AckDeliveredAsync(eventId, [3001, 3002]);

        var db = _redis.Connection.GetDatabase();
        (await db.SetLengthAsync(PhotoRedisKeys.SeasonRolloverEventPending(eventId))).Should().Be(1);
    }

    [SkippableFact]
    public async Task Ack_WhenPendingEmpty_RemovesActiveEvent()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        await ResetStoreAsync();
        var (closedId, newId, eventId) = CreateEventIds();
        var store = CreateStore();
        await store.TryEnqueueBatchAsync(
            CreateRequest(closedId, newId, recipientIds: [4001]),
            TimeSpan.FromDays(45));

        await store.AckDeliveredAsync(eventId, [4001]);

        var db = _redis.Connection.GetDatabase();
        (await db.KeyExistsAsync(PhotoRedisKeys.SeasonRolloverEventMeta(eventId))).Should().BeFalse();
        (await db.KeyExistsAsync(PhotoRedisKeys.SeasonRolloverEventPending(eventId))).Should().BeFalse();
        (await db.SetContainsAsync(PhotoRedisKeys.SeasonRolloverActiveEvents(), eventId)).Should().BeFalse();
    }

    [SkippableFact]
    public async Task Enqueue_IsIdempotent_ForSameEventId()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        await ResetStoreAsync();
        var (closedId, newId, eventId) = CreateEventIds();
        var store = CreateStore();
        var request = CreateRequest(closedId, newId, recipientIds: [5001, 5002]);

        (await store.TryEnqueueBatchAsync(request, TimeSpan.FromDays(45))).Should().Be(2);
        (await store.TryEnqueueBatchAsync(request, TimeSpan.FromDays(45))).Should().Be(0);

        var db = _redis.Connection.GetDatabase();
        (await db.SetLengthAsync(PhotoRedisKeys.SeasonRolloverEventPending(eventId))).Should().Be(2);
    }

    private RedisSeasonRolloverNotificationStore CreateStore() =>
        new(_redis.Connection);

    private static (Guid ClosedId, Guid NewId, string EventId) CreateEventIds()
    {
        var closedId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        return (closedId, newId, SeasonRolloverEventId.Create(closedId, newId));
    }

    private static SeasonRolloverEnqueueRequest CreateRequest(
        Guid closedId,
        Guid newId,
        IReadOnlyList<long> recipientIds) =>
        new()
        {
            ClosedSeasonId = closedId,
            ClosedSeasonName = "Потный июнь",
            ClosedSeasonNumber = 6,
            NewSeasonId = newId,
            NewSeasonName = "Обгоревший июль",
            NewSeasonNumber = 7,
            RecipientTelegramIds = recipientIds
        };
}
