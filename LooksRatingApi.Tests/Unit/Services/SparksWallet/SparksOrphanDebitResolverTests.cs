using FluentAssertions;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class SparksOrphanDebitResolverTests
{
    private const string OrphanKey = "writing-off-sparks:92001:orphan";
    private const string CurrentKey = "writing-off-sparks:92001:current";

    [Fact]
    public async Task ResolveOrphansAsync_CompensatesDebitWithoutWritingOff()
    {
        await using var context = WritingOffSparksCancelFlowHarness.CreateContext();
        var userId = Guid.NewGuid();
        var orphan = SparksDebitIdempotency.Create(userId, OrphanKey, Guid.NewGuid(), 1200m, 100).Value;
        context.SparksDebitIdempotency.Add(orphan);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(userId, 3800m).Value);
        await context.SaveChangesAsync();

        var compensationService = Substitute.For<ICurrencyDebitCompensatedService>();
        var resolver = CreateResolver(context, compensationService);

        await resolver.ResolveOrphansAsync(userId, CurrentKey, CancellationToken.None);

        await compensationService.Received(1).Compensate(
            userId,
            1200m,
            orphan.DebitEventId,
            "orphan_debit_sweep",
            Arg.Any<CancellationToken>());

        var reloaded = await context.SparksDebitIdempotency.SingleAsync();
        reloaded.CompensatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveOrphansAsync_MarksOnly_WhenWritingOffCancelled()
    {
        await using var context = WritingOffSparksCancelFlowHarness.CreateContext();
        var userId = Guid.NewGuid();
        var orphan = SparksDebitIdempotency.Create(userId, OrphanKey, Guid.NewGuid(), 1200m, 100).Value;
        var writingOff = Models.WritingOffSparks.Create(userId, 1200m, OrphanKey, 100, "moscow").Value;
        writingOff.UpdateStatus(OutputStatusEnum.Cancelled);
        context.SparksDebitIdempotency.Add(orphan);
        context.WritingOffSparks.Add(writingOff);
        await context.SaveChangesAsync();

        var compensationService = Substitute.For<ICurrencyDebitCompensatedService>();
        var resolver = CreateResolver(context, compensationService);

        await resolver.ResolveOrphansAsync(userId, CurrentKey, CancellationToken.None);

        await compensationService.DidNotReceive().Compensate(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        var reloaded = await context.SparksDebitIdempotency.SingleAsync();
        reloaded.CompensatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveOrphansAsync_SkipsPendingCreateOnCurrentKey()
    {
        await using var context = WritingOffSparksCancelFlowHarness.CreateContext();
        var userId = Guid.NewGuid();
        var pendingCreate = SparksDebitIdempotency.Create(userId, CurrentKey, Guid.NewGuid(), 1200m, 100).Value;
        context.SparksDebitIdempotency.Add(pendingCreate);
        await context.SaveChangesAsync();

        var compensationService = Substitute.For<ICurrencyDebitCompensatedService>();
        var resolver = CreateResolver(context, compensationService);

        await resolver.ResolveOrphansAsync(userId, CurrentKey, CancellationToken.None);

        await compensationService.DidNotReceive().Compensate(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static SparksOrphanDebitResolver CreateResolver(
        LooksRatingDbContext context,
        ICurrencyDebitCompensatedService compensationService) =>
        new(
            new SparksDebitIdempotencyRepository(context),
            new WritingOffSparksRepository(context),
            compensationService,
            new SparksLedgerRepository(context),
            context,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SparksOrphanDebitResolver>.Instance);
}
