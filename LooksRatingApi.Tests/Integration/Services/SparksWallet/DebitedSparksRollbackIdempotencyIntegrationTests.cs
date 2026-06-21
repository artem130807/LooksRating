using FluentAssertions;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Tests.Infrastructure.Helpers;

namespace LooksRatingApi.Tests.Integration.Services.SparksWallet;

public sealed class DebitedSparksRollbackIdempotencyIntegrationTests
{
    private const long TelegramId = 93_002;
    private const int StarsCount = 100;
    private const decimal SparksCost = 1200m;
    private const decimal InitialBalance = 5000m;
    private const string IdempotencyKey = "writing-off-sparks:93002:callback-rollback";

    [Fact]
    public async Task RollBack_WithSameKey_RestoresBalanceOnce()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness);

        var debit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId, StarsCount, IdempotencyKey, CancellationToken.None);
        debit.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var rollback = await harness.RollBackDebitedSparksOrchestrator.RollBackDebitedSparks(
            TelegramId, StarsCount, "writing_off_failed", IdempotencyKey, CancellationToken.None);
        rollback.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance);

        var replay = await harness.RollBackDebitedSparksOrchestrator.RollBackDebitedSparks(
            TelegramId, StarsCount, "writing_off_failed", IdempotencyKey, CancellationToken.None);
        replay.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance);
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
