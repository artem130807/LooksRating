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
/// End-to-end: sparks debit → withdrawal request → TicketBot-style gRPC cancel → balance restored.
/// </summary>
public sealed class CancelWritingOffSparksRefundsBalanceIntegrationTests
{
    private const long TelegramId = 95_001;
    private const int StarsCount = 100;
    private const decimal SparksCost = 1200m;
    private const decimal InitialBalance = 5000m;
    private const string IdempotencyKey = "writing-off-sparks:95001:callback-e2e";

    [Fact]
    public async Task TicketBotCancelGrpcRequest_RestoresSparksBalance_AfterPendingWithdrawal()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness, InitialBalance);
        var writingOffSparksId = await CreatePendingWithdrawalAsync(harness, user);

        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var grpcResponse = await harness.UpdateStatusGrpcService.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = writingOffSparksId.ToString(),
                Status = GrpcOutputStatusEnum.Cancelled,
            },
            CreateServerCallContext());

        grpcResponse.Success.Should().BeTrue();
        grpcResponse.Message.Should().Be("Статус списания искр обновлён");

        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance);

        var reloaded = await harness.Context.WritingOffSparks
            .AsNoTracking()
            .SingleAsync(x => x.Id == writingOffSparksId);
        reloaded.Status.Should().Be(DomainOutputStatusEnum.Cancelled);
    }

    [Fact]
    public async Task TicketBotCancelGrpcRequest_RejectsSecondCancel_AndKeepsRefundedBalance()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness, InitialBalance);
        var writingOffSparksId = await CreatePendingWithdrawalAsync(harness, user);

        var first = await harness.UpdateStatusGrpcService.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = writingOffSparksId.ToString(),
                Status = GrpcOutputStatusEnum.Cancelled,
            },
            CreateServerCallContext());
        first.Success.Should().BeTrue();

        var second = await harness.UpdateStatusGrpcService.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = writingOffSparksId.ToString(),
                Status = GrpcOutputStatusEnum.Cancelled,
            },
            CreateServerCallContext());

        second.Success.Should().BeFalse();
        second.Message.Should().Be("Статус заявки уже изменён");
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance);
    }

    [Fact]
    public async Task TicketBotConfirmGrpcRequest_DoesNotRestoreSparksBalance()
    {
        await using var harness = WritingOffSparksCancelFlowHarness.Create();
        var user = await SeedUserWithWalletAsync(harness, InitialBalance);
        var writingOffSparksId = await CreatePendingWithdrawalAsync(harness, user);

        var grpcResponse = await harness.UpdateStatusGrpcService.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = writingOffSparksId.ToString(),
                Status = GrpcOutputStatusEnum.Confirmed,
            },
            CreateServerCallContext());

        grpcResponse.Success.Should().BeTrue();
        (await harness.GetBalanceAsync(user.Id)).Should().Be(InitialBalance - SparksCost);

        var reloaded = await harness.Context.WritingOffSparks
            .AsNoTracking()
            .SingleAsync(x => x.Id == writingOffSparksId);
        reloaded.Status.Should().Be(DomainOutputStatusEnum.Confirmed);
    }

    private static async Task<User> SeedUserWithWalletAsync(
        WritingOffSparksCancelFlowHarness harness,
        decimal initialBalance)
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
        await harness.Context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(userId, initialBalance).Value);
        await harness.Context.SaveChangesAsync();
        return user;
    }

    private static async Task<Guid> CreatePendingWithdrawalAsync(
        WritingOffSparksCancelFlowHarness harness,
        User user)
    {
        var debit = await harness.DebitedSparksOrchestrator.DebitedSparks(
            TelegramId,
            StarsCount,
            IdempotencyKey,
            CancellationToken.None);
        debit.Value.Success.Should().BeTrue();

        var writingOff = Models.WritingOffSparks.Create(
            user.Id,
            SparksCost,
            IdempotencyKey,
            StarsCount,
            "moscow").Value;
        await harness.Context.WritingOffSparks.AddAsync(writingOff);
        await harness.Context.SaveChangesAsync();
        return writingOff.Id;
    }

    private static ServerCallContext CreateServerCallContext() =>
        Substitute.For<ServerCallContext>();
}
