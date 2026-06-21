using FluentAssertions;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Tests.Integration.Services.SparksWallet;

public sealed class DebitedSparksIdempotencyIntegrationTests
{
    private const long TelegramId = 93_001;
    private const int StarsCount = 100;
    private const decimal SparksCost = 1200m;
    private const decimal InitialBalance = 5000m;
    private const string IdempotencyKey = "writing-off-sparks:93001:callback-e2e";

    [Fact]
    public async Task DebitedSparks_WithSameKey_DebitsBalanceOnlyOnce()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness);

        var first = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId, StarsCount, IdempotencyKey, CancellationToken.None);
        var second = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId, StarsCount, IdempotencyKey, CancellationToken.None);

        first.Value.Success.Should().BeTrue();
        second.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var records = await harness.Context.SparksDebitIdempotency
            .Where(x => x.UserId == user.Id)
            .ToListAsync();
        records.Should().HaveCount(1);
        records[0].IdempotencyKey.Should().Be(IdempotencyKey);
    }

    [Fact]
    public async Task DebitThenRollbackThenDebitAgain_WithSameKey_RestoresAndRedebitsOnce()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness);

        var firstDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId, StarsCount, IdempotencyKey, CancellationToken.None);
        firstDebit.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var rollback = await harness.RollBackDebitedSparksOrchestrator.RollBackDebitedSparks(
            TelegramId, StarsCount, "writing_off_failed", IdempotencyKey, CancellationToken.None);
        rollback.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance);

        var secondDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId, StarsCount, IdempotencyKey, CancellationToken.None);
        secondDebit.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);
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
