using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Services.Orchestrators;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class CurrentSparksForUserOrchestratorTests
{
    [Fact]
    public async Task ProcessAsync_CheckOnly_WhenAlreadySubscribed_ReturnsAlreadyCredited()
    {
        await using var context = CreateContext();
        var user = CreateUser(88001, subscribed: true);
        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.ProcessAsync(88001, credit: false, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.AlreadyCredited);
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("уже");
    }

    [Fact]
    public async Task ProcessAsync_CheckOnly_WhenNotYetCredited_ReturnsEligible()
    {
        await using var context = CreateContext();
        var user = CreateUser(88002);
        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.ProcessAsync(88002, credit: false, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.Eligible);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Credit_WhenUserNotFound_ReturnsUserNotFound()
    {
        await using var context = CreateContext();
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(99999).Returns((User?)null);
        var orchestrator = CreateOrchestrator(context, userRepository);

        var result = await orchestrator.ProcessAsync(99999, credit: true, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.UserNotFound);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Credit_WhenAlreadySubscribed_ReturnsAlreadyCreditedWithoutProducing()
    {
        await using var context = CreateContext();
        var user = CreateUser(88003, subscribed: true);
        var orchestrator = CreateOrchestrator(context, user, out var producer);

        var result = await orchestrator.ProcessAsync(88003, credit: true, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.AlreadyCredited);
        await producer.DidNotReceive().Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Credit_WhenLockNotAcquired_ReturnsFailed()
    {
        await using var context = CreateContext();
        var user = CreateUser(88006);
        var orchestrator = CreateOrchestrator(
            context,
            user,
            distributedLock: CreateDistributedLock(acquire: false));

        var result = await orchestrator.ProcessAsync(88006, credit: true, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.Failed);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Credit_WhenGrantAlreadyExists_RepairsFlagAndReturnsAlreadyCredited()
    {
        await using var context = CreateContext();
        var product = await TestDataBuilder.SeedVipProductAsync(context);
        var user = CreateUser(88007);
        context.Users.Add(user);
        var payload = ChannelSubscribeSparksRules.BuildPayload(user.TelegramId);
        context.PaymentOrders.Add(
            PaymentOrder.CreateSparksRewardGrant(user.Id, product.Id, payload).Value);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestratorWithTrackedUser(context, user, out var producer, out _);

        var result = await orchestrator.ProcessAsync(88007, credit: true, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.AlreadyCredited);
        (await context.Users.SingleAsync(u => u.TelegramId == 88007)).IssubscribeChannel.Should().BeTrue();
        await producer.DidNotReceive().Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Credit_WhenEligible_CreditsFiftySparksAndMarksSubscribed()
    {
        await using var context = CreateContext();
        await TestDataBuilder.SeedVipProductAsync(context);
        var user = CreateUser(88004);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 10m).Value);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestratorWithTrackedUser(context, user, out var producer, out var eventStore);

        var result = await orchestrator.ProcessAsync(88004, credit: true, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.Credited);
        result.Success.Should().BeTrue();

        var updated = await context.Users.SingleAsync(u => u.TelegramId == 88004);
        updated.IssubscribeChannel.Should().BeTrue();

        var grant = await context.PaymentOrders.SingleAsync();
        grant.Payload.Should().Be(ChannelSubscribeSparksRules.BuildPayload(88004));

        await producer.Received(1).Produce(
            Arg.Is<CurrencySparksEvent>(@event => @event.SparksCount == 60m),
            Arg.Any<CancellationToken>());
        await eventStore.Received(1).SaveEventsAsync(
            Arg.Any<Guid>(),
            Arg.Is<IEnumerable<LooksRatingApi.Domain.Base.DomainEvent>>(
                events => events.OfType<CurrencySparksEvent>().Single().SparksCount == 60m));
    }

    [Fact]
    public async Task ProcessAsync_Credit_WhenSparksServiceFails_RollsBackSubscribeFlagAndGrant()
    {
        await using var context = CreateContext();
        await TestDataBuilder.SeedVipProductAsync(context);
        var user = CreateUser(88005);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id).Value);
        await context.SaveChangesAsync();

        var sparksService = Substitute.For<ICurrencySparksService>();
        sparksService
            .Credited(user.Id, ChannelSubscribeSparksRules.RewardSparks, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("kafka down")));

        var orchestrator = CreateOrchestratorWithTrackedUser(context, user, sparksService: sparksService);

        var result = await orchestrator.ProcessAsync(88005, credit: true, CancellationToken.None);

        result.Status.Should().Be(ChannelSubscribeBonusStatus.Failed);
        result.Success.Should().BeFalse();

        var updated = await context.Users.SingleAsync(u => u.TelegramId == 88005);
        updated.IssubscribeChannel.Should().BeFalse();
        (await context.PaymentOrders.CountAsync()).Should().Be(0);
    }

    private static User CreateUser(long telegramId, bool subscribed = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = VipStatus.Unavaillable,
            IssubscribeChannel = subscribed,
        };

    private static CurrentSparksForUserOrchestrator CreateOrchestratorWithTrackedUser(
        LooksRatingDbContext context,
        User user,
        out IKafkaEventProducer<CurrencySparksEvent> producer,
        out IEventStoreRepository eventStore)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .GetUserByTelegramId(user.TelegramId)
            .Returns(_ => context.Users.First(u => u.TelegramId == user.TelegramId));
        return CreateOrchestrator(context, userRepository, out producer, out eventStore);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestratorWithTrackedUser(
        LooksRatingDbContext context,
        User user,
        ICurrencySparksService sparksService)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .GetUserByTelegramId(user.TelegramId)
            .Returns(_ => context.Users.First(u => u.TelegramId == user.TelegramId));
        return CreateOrchestrator(context, userRepository, sparksService);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User user,
        out IKafkaEventProducer<CurrencySparksEvent> producer)
    {
        return CreateOrchestrator(context, user, out producer, out _);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User user,
        out IKafkaEventProducer<CurrencySparksEvent> producer,
        out IEventStoreRepository eventStore)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(user.TelegramId).Returns(user);
        return CreateOrchestrator(context, userRepository, out producer, out eventStore);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User user,
        ICurrencySparksService sparksService)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(user.TelegramId).Returns(user);
        return CreateOrchestrator(context, userRepository, sparksService);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        IUserRepository userRepository)
    {
        return CreateOrchestrator(context, userRepository, sparksService: null);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User user,
        IRedisDistributedLock? distributedLock = null)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(user.TelegramId).Returns(user);
        return CreateOrchestrator(
            context,
            userRepository,
            out _,
            out _,
            sparksService: null,
            distributedLock: distributedLock);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        IUserRepository userRepository,
        ICurrencySparksService? sparksService)
    {
        return CreateOrchestrator(context, userRepository, out _, out _, sparksService);
    }

    private static CurrentSparksForUserOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        IUserRepository userRepository,
        out IKafkaEventProducer<CurrencySparksEvent> producer,
        out IEventStoreRepository eventStore,
        ICurrencySparksService? sparksService = null,
        IRedisDistributedLock? distributedLock = null)
    {
        producer = Substitute.For<IKafkaEventProducer<CurrencySparksEvent>>();
        eventStore = Substitute.For<IEventStoreRepository>();
        eventStore
            .SaveEventsAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<LooksRatingApi.Domain.Base.DomainEvent>>())
            .Returns(Task.CompletedTask);

        var sparksLedgerRepository = new SparksLedgerRepository(context);
        var walletProvisioner = new SparksWalletProvisioner(
            sparksLedgerRepository,
            NullLogger<SparksWalletProvisioner>.Instance);

        sparksService ??= new CurrencySparksService(
            producer,
            sparksLedgerRepository,
            eventStore,
            walletProvisioner);

        return new CurrentSparksForUserOrchestrator(
            userRepository,
            sparksService,
            walletProvisioner,
            new PaymentOrderRepository(context),
            new ProductRepository(context),
            distributedLock ?? CreateDistributedLock(),
            context,
            NullLogger<CurrentSparksForUserOrchestrator>.Instance);
    }

    private static IRedisDistributedLock CreateDistributedLock(bool acquire = true)
    {
        var distributedLock = Substitute.For<IRedisDistributedLock>();
        distributedLock
            .TryAcquireAsync(
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(acquire ? new TestDistributedLockHandle() : null);

        return distributedLock;
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
