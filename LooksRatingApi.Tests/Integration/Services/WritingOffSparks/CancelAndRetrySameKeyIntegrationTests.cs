using FluentAssertions;
using Grpc.Core;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using LooksRatingGrpc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using DomainOutputStatusEnum = LooksRatingApi.Enums.OutputStatusEnum;
using GrpcOutputStatusEnum = LooksRatingGrpc.OutputStatusEnum;

namespace LooksRatingApi.Tests.Integration.Services.WritingOffSparks;

/// <summary>
/// Admin cancel → user retries the same Telegram callback (same idempotency key).
/// </summary>
public sealed class CancelAndRetrySameKeyIntegrationTests
{
    private const long TelegramId = 96_001;
    private const int StarsCount = 100;
    private const decimal SparksCost = 1200m;
    private const decimal InitialBalance = 5000m;
    private const string IdempotencyKey = "writing-off-sparks:96001:callback-retry";

    [Fact]
    public async Task CancelThenRetrySameKey_DebitsAgainAndReactivatesPendingRequest()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness);

        var firstDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId,
            StarsCount,
            IdempotencyKey,
            CancellationToken.None);
        firstDebit.Value.Success.Should().BeTrue();

        var writingOff = Models.WritingOffSparks.Create(
            user.Id,
            SparksCost,
            IdempotencyKey,
            StarsCount,
            "moscow").Value;
        await harness.Context.WritingOffSparks.AddAsync(writingOff);
        await harness.Context.SaveChangesAsync();
        var writingOffId = writingOff.Id;
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var cancel = await harness.UpdateStatusGrpcService.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = writingOffId.ToString(),
                Status = GrpcOutputStatusEnum.Cancelled,
            },
            CreateServerCallContext());
        cancel.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance);

        var idempotency = await harness.Context.SparksDebitIdempotency
            .SingleAsync(x => x.UserId == user.Id && x.IdempotencyKey == IdempotencyKey);
        idempotency.CompensatedAt.Should().NotBeNull();

        var retryDebit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId,
            StarsCount,
            IdempotencyKey,
            CancellationToken.None);
        retryDebit.Value.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var retryCreate = await harness.CreateWritingOffSparksOrchestrator.ConfirmedWriting(
            TelegramId,
            SparksCost,
            IdempotencyKey,
            StarsCount,
            CancellationToken.None);
        retryCreate.Value.Success.Should().BeTrue();

        (await harness.Context.WritingOffSparks.CountAsync()).Should().Be(1);
        var reloaded = await harness.Context.WritingOffSparks.SingleAsync();
        reloaded.Id.Should().Be(writingOffId);
        reloaded.Status.Should().Be(DomainOutputStatusEnum.Pending);

        var renewedIdempotency = await harness.Context.SparksDebitIdempotency
            .SingleAsync(x => x.UserId == user.Id && x.IdempotencyKey == IdempotencyKey);
        renewedIdempotency.CompensatedAt.Should().BeNull();
    }

    private static async Task<User> SeedUserWithWalletAsync(WritingOffSparksCancelFlowHarness harness)
    {
        var (_, _) = await TestDataBuilder.SeedOpenSeasonAsync(harness.Context);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = TelegramId,
            TelegramUsername = $"user_{TelegramId}",
            Name = $"User {TelegramId}",
            Status = VipStatus.Availlable,
            RecomendationSettings = RecomendationSettings.Create(
                25,
                GenderEnum.Male,
                CityVo.Create("moscow").Value,
                userId).Value,
        };
        harness.Context.Users.Add(user);
        await harness.Context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(userId, InitialBalance).Value);
        await harness.Context.SaveChangesAsync();
        return user;
    }

    private static ServerCallContext CreateServerCallContext() =>
        Substitute.For<ServerCallContext>();
}
