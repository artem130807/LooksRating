using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class CurrencyCreditedSparksByLinkServiceTests
{
    [Fact]
    public async Task CreditReferrerForRegistrationAsync_CreditsFifteenSparksAndIncrementsInviteCount()
    {
        await using var context = CreateContext();
        var referrer = await TestDataBuilder.SeedUserAsync(context, 9101);
        var reference = UserReferenceLink.Create(referrer.Id).Value;
        context.UserReferenceLinks.Add(reference);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(referrer.Id, 5m).Value);
        await context.SaveChangesAsync();

        var newUser = await TestDataBuilder.SeedUserAsync(context, 9102);
        var service = CreateService(
            context,
            out var producer,
            out var eventStore);

        await service.CreditReferrerForRegistrationAsync(
            newUser.Id,
            referrer.Id.ToString(),
            CancellationToken.None);

        (await context.UserReferenceLinks.SingleAsync()).CountInvited.Should().Be(1);

        await producer.Received(1).Produce(
            Arg.Is<CurrencySparksEvent>(@event => @event.SparksCount == 20m),
            Arg.Any<CancellationToken>());
        await eventStore.Received(1).SaveEventsAsync(
            Arg.Any<Guid>(),
            Arg.Is<IEnumerable<LooksRatingApi.Domain.Base.DomainEvent>>(
                events => events.OfType<CurrencySparksEvent>().Single().SparksCount == 20m));
    }

    [Fact]
    public async Task CreditReferrerForRegistrationAsync_ParsesTelegramStartLink()
    {
        await using var context = CreateContext();
        var referrer = await TestDataBuilder.SeedUserAsync(context, 9103);
        var reference = UserReferenceLink.Create(referrer.Id).Value;
        context.UserReferenceLinks.Add(reference);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(referrer.Id).Value);
        await context.SaveChangesAsync();

        var newUser = await TestDataBuilder.SeedUserAsync(context, 9104);
        var service = CreateService(context, out var producer, out _);

        await service.CreditReferrerForRegistrationAsync(
            newUser.Id,
            reference.Link,
            CancellationToken.None);

        await producer.Received(1).Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public async Task CreditReferrerForRegistrationAsync_WhenLinkInvalid_DoesNothing(string? link)
    {
        await using var context = CreateContext();
        var service = CreateService(context, out var producer, out _);

        await service.CreditReferrerForRegistrationAsync(
            Guid.NewGuid(),
            link,
            CancellationToken.None);

        await producer.DidNotReceive().Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreditReferrerForRegistrationAsync_WhenSelfReferral_DoesNothing()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var service = CreateService(context, out var producer, out _);

        await service.CreditReferrerForRegistrationAsync(
            userId,
            userId.ToString(),
            CancellationToken.None);

        await producer.DidNotReceive().Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreditReferrerForRegistrationAsync_WhenInviteLimitReached_DoesNothing()
    {
        await using var context = CreateContext();
        var referrer = await TestDataBuilder.SeedUserAsync(context, 9105);
        var reference = UserReferenceLink.Create(referrer.Id).Value;
        for (var i = 0; i < ReferralSparksRules.MaxInvitedUsers; i++)
        {
            reference.AddCountInvited();
        }

        context.UserReferenceLinks.Add(reference);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(referrer.Id).Value);
        await context.SaveChangesAsync();

        var newUser = await TestDataBuilder.SeedUserAsync(context, 9106);
        var service = CreateService(context, out var producer, out _);

        await service.CreditReferrerForRegistrationAsync(
            newUser.Id,
            referrer.Id.ToString(),
            CancellationToken.None);

        await producer.DidNotReceive().Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
        (await context.UserReferenceLinks.SingleAsync()).CountInvited
            .Should()
            .Be(ReferralSparksRules.MaxInvitedUsers);
    }

    [Fact]
    public async Task CreditReferrerForRegistrationAsync_WhenReferenceMissing_CreatesLinkAndCredits()
    {
        await using var context = CreateContext();
        var referrer = await TestDataBuilder.SeedUserAsync(context, 9107);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(referrer.Id).Value);
        await context.SaveChangesAsync();

        var newUser = await TestDataBuilder.SeedUserAsync(context, 9108);
        var service = CreateService(context, out var producer, out _);

        await service.CreditReferrerForRegistrationAsync(
            newUser.Id,
            referrer.Id.ToString(),
            CancellationToken.None);

        (await context.UserReferenceLinks.CountAsync()).Should().Be(1);
        await producer.Received(1).Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreditReferrerForRegistrationAsync_WhenFifthInvite_Succeeds()
    {
        await using var context = CreateContext();
        var referrer = await TestDataBuilder.SeedUserAsync(context, 9109);
        var reference = UserReferenceLink.Create(referrer.Id).Value;
        for (var i = 0; i < ReferralSparksRules.MaxInvitedUsers - 1; i++)
        {
            reference.AddCountInvited();
        }

        context.UserReferenceLinks.Add(reference);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(referrer.Id).Value);
        await context.SaveChangesAsync();

        var newUser = await TestDataBuilder.SeedUserAsync(context, 9110);
        var service = CreateService(context, out var producer, out _);

        await service.CreditReferrerForRegistrationAsync(
            newUser.Id,
            referrer.Id.ToString(),
            CancellationToken.None);

        await producer.Received(1).Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
        (await context.UserReferenceLinks.SingleAsync()).CountInvited
            .Should()
            .Be(ReferralSparksRules.MaxInvitedUsers);
        (await context.ReferralInvites.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreditReferrerForRegistrationAsync_WhenSameInvitedUserTwice_CreditsOnlyOnce()
    {
        await using var context = CreateContext();
        var referrer = await TestDataBuilder.SeedUserAsync(context, 9111);
        var reference = UserReferenceLink.Create(referrer.Id).Value;
        context.UserReferenceLinks.Add(reference);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(referrer.Id).Value);
        await context.SaveChangesAsync();

        var invitedUserId = Guid.NewGuid();
        var service = CreateService(context, out var producer, out _);

        await service.CreditReferrerForRegistrationAsync(
            invitedUserId,
            referrer.Id.ToString(),
            CancellationToken.None);
        await service.CreditReferrerForRegistrationAsync(
            invitedUserId,
            referrer.Id.ToString(),
            CancellationToken.None);

        await producer.Received(1).Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
        (await context.UserReferenceLinks.SingleAsync()).CountInvited.Should().Be(1);
    }

    [Fact]
    public async Task CreditReferrerForRegistrationAsync_WhenCreditFails_ReleasesReservationAndDoesNotThrow()
    {
        await using var context = CreateContext();
        var referrer = await TestDataBuilder.SeedUserAsync(context, 9112);
        var reference = UserReferenceLink.Create(referrer.Id).Value;
        context.UserReferenceLinks.Add(reference);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(referrer.Id).Value);
        await context.SaveChangesAsync();

        var newUser = await TestDataBuilder.SeedUserAsync(context, 9113);
        var producer = Substitute.For<IKafkaEventProducer<CurrencySparksEvent>>();
        producer
            .Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("kafka down"));

        var service = CreateService(context, producer, Substitute.For<IEventStoreRepository>());

        var act = async () => await service.CreditReferrerForRegistrationAsync(
            newUser.Id,
            referrer.Id.ToString(),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        (await context.UserReferenceLinks.SingleAsync()).CountInvited.Should().Be(0);
        (await context.ReferralInvites.CountAsync()).Should().Be(0);
        await producer.Received(1).Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());
    }

    private static CurrencyCreditedSparksByLinkService CreateService(
        LooksRatingDbContext context,
        out IKafkaEventProducer<CurrencySparksEvent> producer,
        out IEventStoreRepository eventStore)
    {
        eventStore = Substitute.For<IEventStoreRepository>();
        producer = Substitute.For<IKafkaEventProducer<CurrencySparksEvent>>();
        return CreateService(context, producer, eventStore);
    }

    private static CurrencyCreditedSparksByLinkService CreateService(
        LooksRatingDbContext context,
        IKafkaEventProducer<CurrencySparksEvent> producer,
        IEventStoreRepository eventStore)
    {
        var sparksLedgerRepository = new SparksLedgerRepository(context);
        var provisioner = new SparksWalletProvisioner(
            sparksLedgerRepository,
            NullLogger<SparksWalletProvisioner>.Instance);
        var currencySparksService = new CurrencySparksService(
            producer,
            sparksLedgerRepository,
            eventStore,
            provisioner);

        return new CurrencyCreditedSparksByLinkService(
            new UserReferenceLinkRepository(context),
            new UserRepository(context),
            currencySparksService,
            provisioner,
            NullLogger<CurrencyCreditedSparksByLinkService>.Instance);
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
