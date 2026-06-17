using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class RatedProfileSparksRewardServiceTests
{
    [Fact]
    public async Task TryAwardForRatedProfileAsync_WhenWalletMissing_CreatesWalletAndCreditsHalfSpark()
    {
        await using var context = CreateContext();
        var user = await TestDataBuilder.SeedUserAsync(context, 904);

        var sparksLedgerRepository = new SparksLedgerRepository(context);
        var provisioner = new SparksWalletProvisioner(
            sparksLedgerRepository,
            NullLogger<SparksWalletProvisioner>.Instance);

        var eventStore = Substitute.For<IEventStoreRepository>();
        var producer = Substitute.For<IKafkaEventProducer<CurrencySparksEvent>>();
        var currencySparksService = new CurrencySparksService(
            producer,
            sparksLedgerRepository,
            eventStore,
            provisioner);

        var redis = CreateRedisMock(awardedToday: 0);
        var service = new RatedProfileSparksRewardService(
            currencySparksService,
            provisioner,
            redis,
            NullLogger<RatedProfileSparksRewardService>.Instance);

        await service.TryAwardForRatedProfileAsync(user.TelegramId, user.Id, CancellationToken.None);

        (await context.SparksLedgers.AnyAsync(wallet => wallet.UserId == user.Id)).Should().BeTrue();

        await producer.Received(1).Produce(
            Arg.Is<CurrencySparksEvent>(@event => @event.SparksCount == 0.5m),
            Arg.Any<CancellationToken>());
        await eventStore.Received(1).SaveEventsAsync(
            Arg.Any<Guid>(),
            Arg.Is<IEnumerable<LooksRatingApi.Domain.Base.DomainEvent>>(
                events => events.OfType<CurrencySparksEvent>().Single().SparksCount == 0.5m));
    }

    [Fact]
    public async Task TryAwardForRatedProfileAsync_WhenDailyLimitReached_DoesNotCreditSparks()
    {
        await using var context = CreateContext();
        var user = await TestDataBuilder.SeedUserAsync(context, 905);

        var sparksLedgerRepository = new SparksLedgerRepository(context);
        var provisioner = new SparksWalletProvisioner(
            sparksLedgerRepository,
            NullLogger<SparksWalletProvisioner>.Instance);

        var eventStore = Substitute.For<IEventStoreRepository>();
        var producer = Substitute.For<IKafkaEventProducer<CurrencySparksEvent>>();
        var currencySparksService = new CurrencySparksService(
            producer,
            sparksLedgerRepository,
            eventStore,
            provisioner);

        var redis = CreateRedisMock(awardedToday: 15);
        var service = new RatedProfileSparksRewardService(
            currencySparksService,
            provisioner,
            redis,
            NullLogger<RatedProfileSparksRewardService>.Instance);

        await service.TryAwardForRatedProfileAsync(user.TelegramId, user.Id, CancellationToken.None);

        await producer.DidNotReceive().Produce(
            Arg.Any<CurrencySparksEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryAwardForRatedProfileAsync_WhenIdentifiersInvalid_DoesNotCreditSparks()
    {
        var producer = Substitute.For<IKafkaEventProducer<CurrencySparksEvent>>();
        var currencySparksService = Substitute.For<ICurrencySparksService>();
        var provisioner = Substitute.For<ISparksWalletProvisioner>();
        var redis = Substitute.For<IConnectionMultiplexer>();

        var service = new RatedProfileSparksRewardService(
            currencySparksService,
            provisioner,
            redis,
            NullLogger<RatedProfileSparksRewardService>.Instance);

        await service.TryAwardForRatedProfileAsync(0, Guid.Empty, CancellationToken.None);

        await currencySparksService.DidNotReceive().Credited(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<CancellationToken>());
    }

    private static IConnectionMultiplexer CreateRedisMock(int awardedToday)
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)awardedToday);
        return redis;
    }

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
