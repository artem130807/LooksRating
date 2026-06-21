using FluentAssertions;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WritingOffSparksEntity = LooksRatingApi.Models.WritingOffSparks;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class UpdateStatusWritingOffSparksOrchestratorTests
{
    [Theory]
    [InlineData(OutputStatusEnum.Confirmed)]
    [InlineData(OutputStatusEnum.Cancelled)]
    public async Task UpdateStatusAsync_UpdatesPersistedStatus_WhenPending(OutputStatusEnum newStatus)
    {
        await using var context = CreateContext();
        var writingOffSparks = await SeedWritingOffSparksAsync(context, OutputStatusEnum.Pending);
        var orchestrator = CreateOrchestrator(context);

        var result = await orchestrator.UpdateStatusAsync(
            writingOffSparks.Id,
            newStatus,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.Message.Should().Be("Статус списания искр обновлён");

        var reloaded = await context.WritingOffSparks
            .AsNoTracking()
            .SingleAsync(x => x.Id == writingOffSparks.Id);
        reloaded.Status.Should().Be(newStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNotFound_WhenEntityMissing()
    {
        await using var context = CreateContext();
        var orchestrator = CreateOrchestrator(context);

        var result = await orchestrator.UpdateStatusAsync(
            Guid.NewGuid(),
            OutputStatusEnum.Confirmed,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Списание искр не найдено");
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsFailedStatus()
    {
        await using var context = CreateContext();
        var writingOffSparks = await SeedWritingOffSparksAsync(context, OutputStatusEnum.Pending);
        var orchestrator = CreateOrchestrator(context);

        var result = await orchestrator.UpdateStatusAsync(
            writingOffSparks.Id,
            OutputStatusEnum.Failed,
            CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Допустимы только статусы «Выполнена» и «Отменена»");
    }

    [Fact]
    public async Task UpdateStatusAsync_Rejects_WhenAlreadyProcessed()
    {
        await using var context = CreateContext();
        var writingOffSparks = await SeedWritingOffSparksAsync(context, OutputStatusEnum.Confirmed);
        var orchestrator = CreateOrchestrator(context);

        var result = await orchestrator.UpdateStatusAsync(
            writingOffSparks.Id,
            OutputStatusEnum.Cancelled,
            CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Статус заявки уже изменён");
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsFailure_WhenStatusIsInvalid()
    {
        await using var context = CreateContext();
        var writingOffSparks = await SeedWritingOffSparksAsync(context, OutputStatusEnum.Pending);
        var orchestrator = CreateOrchestrator(context);
        var invalidStatus = (OutputStatusEnum)999;

        var result = await orchestrator.UpdateStatusAsync(
            writingOffSparks.Id,
            invalidStatus,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Некорректный статус списания искр");

        var reloaded = await context.WritingOffSparks
            .AsNoTracking()
            .SingleAsync(x => x.Id == writingOffSparks.Id);
        reloaded.Status.Should().Be(OutputStatusEnum.Pending);
    }

    [Fact]
    public async Task UpdateStatusAsync_RefundsSparks_WhenCancelled()
    {
        await using var context = CreateContext();
        var writingOffSparks = await SeedWritingOffSparksAsync(context, OutputStatusEnum.Pending);
        var compensationService = Substitute.For<ICurrencyDebitCompensatedService>();
        var orchestrator = CreateOrchestrator(context, compensationService);

        var result = await orchestrator.UpdateStatusAsync(
            writingOffSparks.Id,
            OutputStatusEnum.Cancelled,
            CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await compensationService.Received(1).Compensate(
            writingOffSparks.UserId,
            1200m,
            writingOffSparks.Id,
            "writing_off_cancelled",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_MarksDebitIdempotencyCompensated_WhenCancelled()
    {
        await using var context = CreateContext();
        var writingOffSparks = await SeedWritingOffSparksAsync(context, OutputStatusEnum.Pending);
        var idempotency = SparksDebitIdempotency.Create(
            writingOffSparks.UserId,
            writingOffSparks.IdempotencyKey,
            Guid.NewGuid(),
            1200m,
            100).Value;
        context.SparksDebitIdempotency.Add(idempotency);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(context);

        var result = await orchestrator.UpdateStatusAsync(
            writingOffSparks.Id,
            OutputStatusEnum.Cancelled,
            CancellationToken.None);

        result.Value.Success.Should().BeTrue();

        var reloaded = await context.SparksDebitIdempotency.SingleAsync(x => x.Id == idempotency.Id);
        reloaded.CompensatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_DoesNotRefundSparks_WhenConfirmed()
    {
        await using var context = CreateContext();
        var writingOffSparks = await SeedWritingOffSparksAsync(context, OutputStatusEnum.Pending);
        var compensationService = Substitute.For<ICurrencyDebitCompensatedService>();
        var orchestrator = CreateOrchestrator(context, compensationService);

        var result = await orchestrator.UpdateStatusAsync(
            writingOffSparks.Id,
            OutputStatusEnum.Confirmed,
            CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await compensationService.DidNotReceive().Compensate(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static UpdateStatusWritingOffSparksOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        ICurrencyDebitCompensatedService? compensationService = null) =>
        new(
            new WritingOffSparksRepository(context),
            compensationService ?? Substitute.For<ICurrencyDebitCompensatedService>(),
            new SparksLedgerRepository(context),
            new SparksDebitIdempotencyRepository(context),
            context,
            NullLogger<UpdateStatusWritingOffSparksOrchestrator>.Instance);

    private static async Task<WritingOffSparksEntity> SeedWritingOffSparksAsync(
        LooksRatingDbContext context,
        OutputStatusEnum status)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = Random.Shared.NextInt64(100_000, 999_999),
            TelegramUsername = "writing_off_user",
            Name = "Writing Off User",
        };

        var writingOffSparks = WritingOffSparksEntity.Create(
            user.Id,
            1200m,
            $"test-key-{user.TelegramId}",
            100,
            "moscow").Value;
        writingOffSparks.UpdateStatus(status);

        context.Users.Add(user);
        context.WritingOffSparks.Add(writingOffSparks);
        await context.SaveChangesAsync();

        return writingOffSparks;
    }

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
