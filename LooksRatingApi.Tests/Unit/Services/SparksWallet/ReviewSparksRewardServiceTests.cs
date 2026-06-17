using LooksRatingApi;
using LooksRatingApi.Contracts;
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

public sealed class ReviewSparksRewardServiceTests
{
    [Fact]
    public async Task TryAwardForReviewAsync_WhenWalletMissing_CreatesWalletAndCreditsSparks()
    {
        await using var context = CreateContext();
        var user = await TestDataBuilder.SeedUserAsync(context, 903);

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
        var service = new ReviewSparksRewardService(
            currencySparksService,
            provisioner,
            redis,
            NullLogger<ReviewSparksRewardService>.Instance);

        await service.TryAwardForReviewAsync(user.TelegramId, user.Id, CancellationToken.None);

        (await context.SparksLedgers.AnyAsync(wallet => wallet.UserId == user.Id)).Should().BeTrue();

        await producer.Received(1).Produce(
            Arg.Is<CurrencySparksEvent>(@event => @event.SparksCount == 1m),
            Arg.Any<CancellationToken>());
        await eventStore.Received(1).SaveEventsAsync(
            Arg.Any<Guid>(),
            Arg.Is<IEnumerable<LooksRatingApi.Domain.Base.DomainEvent>>(
                events => events.OfType<CurrencySparksEvent>().Single().SparksCount == 1m));
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
