using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using LooksRatingApi.Services.SparksLedger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class RollBackDebitedSparksOrchestratorTests
{
    private const string IdempotencyKey = "writing-off-sparks:92001:callback-1";

    [Fact]
    public async Task RollBack_WithKey_CompensatesByStoredEventId_NotLastDebit()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(92001);
        var wallet = LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value;
        context.Users.Add(user);
        context.SparksLedgers.Add(wallet);
        await context.SaveChangesAsync();

        var firstEventId = Guid.NewGuid();
        var secondEventId = Guid.NewGuid();
        var idempotencyRecord = SparksDebitIdempotency.Create(
            user.Id,
            IdempotencyKey,
            firstEventId,
            1200m,
            100).Value;
        context.SparksDebitIdempotency.Add(idempotencyRecord);
        await context.SaveChangesAsync();

        var compensateService = Substitute.For<ICurrencyDebitCompensatedService>();
        var eventStore = Substitute.For<IEventStoreRepository>();
        eventStore.GetLastEvent(wallet.Id).Returns(new CurrencyDebitedEvent(wallet.Id, 3800m) { EventId = secondEventId });

        var orchestrator = CreateOrchestrator(context, user, compensateService, eventStore);

        var result = await orchestrator.RollBackDebitedSparks(
            92001,
            100,
            "writing_off_failed",
            IdempotencyKey,
            CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await compensateService.Received(1).Compensate(
            user.Id,
            1200m,
            firstEventId,
            "writing_off_failed",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollBack_WithSameKey_IsIdempotent()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(92002);
        var wallet = LooksRatingApi.Models.SparksWallet.Create(user.Id, 3800m).Value;
        context.Users.Add(user);
        context.SparksLedgers.Add(wallet);

        var debitEventId = Guid.NewGuid();
        var idempotencyRecord = SparksDebitIdempotency.Create(
            user.Id,
            IdempotencyKey,
            debitEventId,
            1200m,
            100).Value;
        idempotencyRecord.MarkCompensated();
        context.SparksDebitIdempotency.Add(idempotencyRecord);
        await context.SaveChangesAsync();

        var compensateService = Substitute.For<ICurrencyDebitCompensatedService>();
        var orchestrator = CreateOrchestrator(context, user, compensateService);

        var first = await orchestrator.RollBackDebitedSparks(
            92002, 100, "writing_off_failed", IdempotencyKey, CancellationToken.None);
        var second = await orchestrator.RollBackDebitedSparks(
            92002, 100, "writing_off_failed", IdempotencyKey, CancellationToken.None);

        first.Value.Success.Should().BeTrue();
        second.Value.Success.Should().BeTrue();
        await compensateService.DidNotReceive().Compensate(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollBack_WithoutKey_UsesLastEvent()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(92003);
        var wallet = LooksRatingApi.Models.SparksWallet.Create(user.Id, 3800m).Value;
        context.Users.Add(user);
        context.SparksLedgers.Add(wallet);
        await context.SaveChangesAsync();

        var lastEventId = Guid.NewGuid();
        var compensateService = Substitute.For<ICurrencyDebitCompensatedService>();
        var eventStore = Substitute.For<IEventStoreRepository>();
        eventStore.GetLastEvent(wallet.Id).Returns(new CurrencyDebitedEvent(wallet.Id, 3800m) { EventId = lastEventId });

        var orchestrator = CreateOrchestrator(context, user, compensateService, eventStore);

        var result = await orchestrator.RollBackDebitedSparks(
            92003, 100, "gift_delivery_failed", null, CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await compensateService.Received(1).Compensate(
            user.Id,
            1200m,
            lastEventId,
            "gift_delivery_failed",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollBack_WithKey_WhenNoRecord_Fails()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(92004);
        var wallet = LooksRatingApi.Models.SparksWallet.Create(user.Id, 3800m).Value;
        context.Users.Add(user);
        context.SparksLedgers.Add(wallet);
        await context.SaveChangesAsync();

        var compensateService = Substitute.For<ICurrencyDebitCompensatedService>();
        var eventStore = Substitute.For<IEventStoreRepository>();
        eventStore.GetLastEvent(wallet.Id).Returns(new CurrencyDebitedEvent(wallet.Id, 3800m));

        var orchestrator = CreateOrchestrator(context, user, compensateService, eventStore);

        var result = await orchestrator.RollBackDebitedSparks(
            92004, 100, "writing_off_failed", IdempotencyKey, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        await compensateService.DidNotReceive().Compensate(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
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

    private static RollBackDebitedSparksOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User user,
        ICurrencyDebitCompensatedService compensateService,
        IEventStoreRepository? eventStore = null)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .GetUserByTelegramId(user.TelegramId)
            .Returns(_ => context.Users.First(u => u.TelegramId == user.TelegramId));

        eventStore ??= Substitute.For<IEventStoreRepository>();

        return new RollBackDebitedSparksOrchestrator(
            compensateService,
            eventStore,
            NullLogger<RollBackDebitedSparksOrchestrator>.Instance,
            context,
            userRepository,
            new SparksLedgerRepository(context),
            new SparksDebitIdempotencyRepository(context));
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
