using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Services;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Integration.Processors;

[Collection(IntegrationCollection.Name)]
public sealed class VipStatusExpiryProcessorTests
{
    private readonly PostgresFixture _postgres;
    private readonly RedisFixture _redis;

    public VipStatusExpiryProcessorTests(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
    }

    [SkippableFact]
    public async Task ProcessAsync_DeactivatesExpiredVipUsers()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var product = await TestDataBuilder.SeedVipProductAsync(context);
        var expiredUser = await TestDataBuilder.SeedUserAsync(context, 5001, VipStatus.Availlable);
        var activeUser = await TestDataBuilder.SeedUserAsync(context, 5002, VipStatus.Availlable);

        await TestDataBuilder.SeedPaidVipOrderAsync(
            context,
            expiredUser,
            product,
            DateTime.UtcNow.AddDays(-31));
        await TestDataBuilder.SeedPaidVipOrderAsync(
            context,
            activeUser,
            product,
            DateTime.UtcNow.AddDays(-5));

        var processor = CreateProcessor(context);
        await processor.ProcessAsync(CancellationToken.None);

        var statuses = await context.Users
            .Where(user => user.TelegramId == 5001 || user.TelegramId == 5002)
            .ToDictionaryAsync(user => user.TelegramId, user => user.Status);

        statuses[5001].Should().Be(VipStatus.Unavaillable);
        statuses[5002].Should().Be(VipStatus.Availlable);
    }

    [SkippableFact]
    public async Task ProcessAsync_WhenLockIsHeld_SkipsWork()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres, _redis);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var product = await TestDataBuilder.SeedVipProductAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 5003, VipStatus.Availlable);
        await TestDataBuilder.SeedPaidVipOrderAsync(
            context,
            user,
            product,
            DateTime.UtcNow.AddDays(-40));

        var distributedLock = new RedisDistributedLock(_redis.Connection);
        await using var held = await distributedLock.TryAcquireAsync(
            DistributedLockKeys.VipStatusExpiry,
            TimeSpan.FromMinutes(1));

        held.Should().NotBeNull();

        var processor = CreateProcessor(context, distributedLock);
        await processor.ProcessAsync(CancellationToken.None);

        var status = await context.Users
            .Where(u => u.TelegramId == 5003)
            .Select(u => u.Status)
            .SingleAsync();

        status.Should().Be(VipStatus.Availlable);
    }

    private VipStatusExpiryProcessor CreateProcessor(
        LooksRatingApi.LooksRatingDbContext context,
        IRedisDistributedLock? distributedLock = null)
    {
        return new VipStatusExpiryProcessor(
            context,
            new VipExpirationReadService(context),
            distributedLock ?? new RedisDistributedLock(_redis.Connection),
            NullLogger<VipStatusExpiryProcessor>.Instance);
    }
}
