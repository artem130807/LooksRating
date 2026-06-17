using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Tests.Infrastructure.Fixtures;

namespace LooksRatingApi.Tests.Integration.DistributedLock;

[Collection(IntegrationCollection.Name)]
public sealed class RedisDistributedLockTests
{
    private readonly RedisFixture _redis;

    public RedisDistributedLockTests(RedisFixture redis)
    {
        _redis = redis;
    }

    [SkippableFact]
    public async Task TryAcquire_AllowsOnlyOneHolder()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var lockService = new RedisDistributedLock(_redis.Connection);
        const string key = "looksrating:test:lock:single";

        await using var first = await lockService.TryAcquireAsync(key, TimeSpan.FromMinutes(1));
        var second = await lockService.TryAcquireAsync(key, TimeSpan.FromMinutes(1));

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [SkippableFact]
    public async Task Release_AllowsReacquire()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var lockService = new RedisDistributedLock(_redis.Connection);
        const string key = "looksrating:test:lock:release";

        var first = await lockService.TryAcquireAsync(key, TimeSpan.FromMinutes(1));
        first.Should().NotBeNull();
        await first!.DisposeAsync();

        var second = await lockService.TryAcquireAsync(key, TimeSpan.FromMinutes(1));
        second.Should().NotBeNull();
        await second!.DisposeAsync();
    }

    [SkippableFact]
    public async Task Renew_KeepsLockForHolder()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var lockService = new RedisDistributedLock(_redis.Connection);
        const string key = "looksrating:test:lock:renew";

        await using var handle = await lockService.TryAcquireAsync(key, TimeSpan.FromSeconds(2));
        handle.Should().NotBeNull();

        (await handle!.RenewAsync(TimeSpan.FromMinutes(1))).Should().BeTrue();
        (await lockService.IsLockedAsync(key)).Should().BeTrue();

        var competitor = await lockService.TryAcquireAsync(key, TimeSpan.FromMinutes(1));
        competitor.Should().BeNull();
    }

    [SkippableFact]
    public async Task Renew_WithForeignToken_Fails()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_redis);
        var lockService = new RedisDistributedLock(_redis.Connection);
        const string key = "looksrating:test:lock:foreign";

        await using var handle = await lockService.TryAcquireAsync(key, TimeSpan.FromMinutes(1));
        handle.Should().NotBeNull();

        (await lockService.RenewAsync(key, "foreign-token", TimeSpan.FromMinutes(1))).Should().BeFalse();
        (await handle!.RenewAsync(TimeSpan.FromMinutes(1))).Should().BeTrue();
    }
}
