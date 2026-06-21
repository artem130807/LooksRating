using FluentAssertions;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Tests.Integration.Services.SparksWallet;

/// <summary>
/// Debit succeeded, create never persisted, rollback failed → new exchange with another key.
/// </summary>
public sealed class OrphanDebitSweepIntegrationTests
{
    private const long TelegramId = 97_001;
    private const int StarsCount = 100;
    private const decimal SparksCost = 1200m;
    private const decimal InitialBalance = 5000m;
    private const string OrphanKey = "writing-off-sparks:97001:orphan-key";
    private const string NewKey = "writing-off-sparks:97001:new-key";

    [Fact]
    public async Task NewDebit_CompensatesOrphanBeforeDebitingAgain()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness);

        var orphanDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId,
            StarsCount,
            OrphanKey,
            CancellationToken.None);
        orphanDebit.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var newDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId,
            StarsCount,
            NewKey,
            CancellationToken.None);
        newDebit.Value.Success.Should().BeTrue();

        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var records = await harness.Context.SparksDebitIdempotency
            .Where(x => x.UserId == user.Id)
            .ToListAsync();
        records.Should().HaveCount(2);

        var orphanRecord = records.Single(x => x.IdempotencyKey == OrphanKey);
        orphanRecord.CompensatedAt.Should().NotBeNull();

        var newRecord = records.Single(x => x.IdempotencyKey == NewKey);
        newRecord.CompensatedAt.Should().BeNull();
    }

    [Fact]
    public async Task SameKeyRetryAfterOrphan_DoesNotDoubleDebitBeforeCreate()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness);

        var firstDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId,
            StarsCount,
            OrphanKey,
            CancellationToken.None);
        firstDebit.Value.Success.Should().BeTrue();

        var replayDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId,
            StarsCount,
            OrphanKey,
            CancellationToken.None);
        replayDebit.Value.Success.Should().BeTrue();
        replayDebit.Value.Message.Should().Contain("уже");

        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var record = await harness.Context.SparksDebitIdempotency.SingleAsync();
        record.CompensatedAt.Should().BeNull();
    }

    private static async Task<User> SeedUserWithWalletAsync(WritingOffSparksCancelFlowHarness harness)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = TelegramId,
            TelegramUsername = $"user_{TelegramId}",
            Name = $"User {TelegramId}",
            Status = VipStatus.Availlable,
        };
        harness.Context.Users.Add(user);
        await harness.Context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(userId, InitialBalance).Value);
        await harness.Context.SaveChangesAsync();
        return user;
    }
}
