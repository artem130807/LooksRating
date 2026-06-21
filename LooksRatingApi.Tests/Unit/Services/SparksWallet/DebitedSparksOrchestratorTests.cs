using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class DebitedSparksOrchestratorTests
{
    private const string IdempotencyKey = "writing-off-sparks:91005:callback-1";

    [Theory]
    [InlineData(50)]
    [InlineData(150)]
    [InlineData(500)]
    public async Task DebitedSparks_RejectsUnknownStarTier(int starsCount)
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91001);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.DebitedSparks(91001, starsCount, null, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Недопустимая стоимость подарка");
    }

    [Fact]
    public async Task DebitedSparks_RejectsInsufficientBalanceAtTwelveToOneRate()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91002);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 1199m).Value);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.DebitedSparks(91002, 100, null, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Недостаточно искр на балансе");
    }

    [Fact]
    public async Task DebitedSparks_RejectsMissingWallet()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91004);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.DebitedSparks(91004, 100, null, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Кошелёк искр не найден");
    }

    [Fact]
    public async Task DebitedSparks_DebitsTwelveSparksPerStarOnSuccess()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91003);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 1200m).Value);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        debitService
            .Debited(user.Id, 1200m, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var orchestrator = CreateOrchestrator(context, user, debitService);

        var result = await orchestrator.DebitedSparks(91003, 100, null, CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await debitService.Received(1).Debited(user.Id, 1200m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DebitedSparks_WithSameKey_DebitsOnlyOnce()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91005);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        debitService
            .Debited(user.Id, 1200m, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var orchestrator = CreateOrchestrator(context, user, debitService);

        var first = await orchestrator.DebitedSparks(91005, 100, IdempotencyKey, CancellationToken.None);
        var second = await orchestrator.DebitedSparks(91005, 100, IdempotencyKey, CancellationToken.None);

        first.Value.Success.Should().BeTrue();
        second.Value.Success.Should().BeTrue();
        await debitService.Received(1).Debited(user.Id, 1200m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DebitedSparks_WithSameKey_ReturnsSuccessOnReplay()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91006);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        debitService
            .Debited(user.Id, 1200m, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var orchestrator = CreateOrchestrator(context, user, debitService);

        await orchestrator.DebitedSparks(91006, 100, IdempotencyKey, CancellationToken.None);
        var replay = await orchestrator.DebitedSparks(91006, 100, IdempotencyKey, CancellationToken.None);

        replay.Value.Success.Should().BeTrue();
        replay.Value.Message.Should().Contain("уже");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DebitedSparks_RejectsInvalidKey(string invalidKey)
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91007);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.DebitedSparks(91007, 100, invalidKey, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Contain("Ключ");
    }

    [Fact]
    public async Task DebitedSparks_WhenWritingOffSparksExistsForKey_SkipsDebit()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91008);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        var writingOff = Models.WritingOffSparks.Create(user.Id, 1200m, IdempotencyKey, 100, "moscow").Value;
        await context.WritingOffSparks.AddAsync(writingOff);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        var orchestrator = CreateOrchestrator(context, user, debitService);

        var result = await orchestrator.DebitedSparks(91008, 100, IdempotencyKey, CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await debitService.DidNotReceive().Debited(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DebitedSparks_WhenWritingOffSparksCancelled_DebitsAgainWithSameKey()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91011);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        var writingOff = Models.WritingOffSparks.Create(user.Id, 1200m, IdempotencyKey, 100, "moscow").Value;
        writingOff.UpdateStatus(OutputStatusEnum.Cancelled);
        context.SparksDebitIdempotency.Add(SparksDebitIdempotency.Create(
            user.Id,
            IdempotencyKey,
            Guid.NewGuid(),
            1200m,
            100).Value);
        await context.WritingOffSparks.AddAsync(writingOff);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        debitService
            .Debited(user.Id, 1200m, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        var orchestrator = CreateOrchestrator(context, user, debitService);

        var result = await orchestrator.DebitedSparks(91011, 100, IdempotencyKey, CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await debitService.Received(1).Debited(user.Id, 1200m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DebitedSparks_WithSameKey_RejectsMismatchedStarsCount()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91009);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        var existing = SparksDebitIdempotency.Create(
            user.Id,
            IdempotencyKey,
            Guid.NewGuid(),
            1200m,
            100).Value;
        context.SparksDebitIdempotency.Add(existing);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        var orchestrator = CreateOrchestrator(context, user, debitService);

        var result = await orchestrator.DebitedSparks(91009, 200, IdempotencyKey, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Недопустимая стоимость подарка");
        await debitService.DidNotReceive().Debited(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DebitedSparks_AfterCompensation_DebitsAgainWithSameKey()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91010);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        var compensated = SparksDebitIdempotency.Create(
            user.Id,
            IdempotencyKey,
            Guid.NewGuid(),
            1200m,
            100).Value;
        compensated.MarkCompensated();
        context.SparksDebitIdempotency.Add(compensated);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        var newEventId = Guid.NewGuid();
        debitService
            .Debited(user.Id, 1200m, Arg.Any<CancellationToken>())
            .Returns(newEventId);

        var orchestrator = CreateOrchestrator(context, user, debitService);

        var result = await orchestrator.DebitedSparks(91010, 100, IdempotencyKey, CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await debitService.Received(1).Debited(user.Id, 1200m, Arg.Any<CancellationToken>());

        var reloaded = await context.SparksDebitIdempotency.SingleAsync(
            x => x.UserId == user.Id && x.IdempotencyKey == IdempotencyKey);
        reloaded.CompensatedAt.Should().BeNull();
    }

    private static User CreateVipUser(long telegramId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = VipStatus.Availlable,
        };

    private static DebitedSparksOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User user,
        ICurrencyDebitedService? debitService = null)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .GetUserByTelegramId(user.TelegramId)
            .Returns(_ => context.Users.First(u => u.TelegramId == user.TelegramId));

        debitService ??= Substitute.For<ICurrencyDebitedService>();
        debitService
            .Debited(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var orphanResolver = Substitute.For<ISparksOrphanDebitResolver>();
        orphanResolver
            .ResolveOrphansAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new DebitedSparksOrchestrator(
            debitService,
            NullLogger<DebitedSparksOrchestrator>.Instance,
            context,
            userRepository,
            new SparksLedgerRepository(context),
            new SparksDebitIdempotencyRepository(context),
            new WritingOffSparksRepository(context),
            orphanResolver);
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
