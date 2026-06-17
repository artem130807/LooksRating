using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.Users.Command.RegisterUser;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Integration.Cqrs;

[Collection(IntegrationCollection.Name)]
public sealed class ReferralSparksIntegrationTests
{
    private readonly PostgresFixture _postgres;

    public ReferralSparksIntegrationTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [SkippableFact]
    public async Task CreditReferrerForRegistrationAsync_RespectsFiveInviteLimit_OnPostgres()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var referrer = await TestDataBuilder.SeedUserAsync(context, 9301);
        await context.SparksLedgers.AddAsync(SparksWallet.Create(referrer.Id, 0m).Value);
        context.UserReferenceLinks.Add(UserReferenceLink.Create(referrer.Id).Value);
        await context.SaveChangesAsync();

        var producer = Substitute.For<IKafkaEventProducer<CurrencySparksEvent>>();
        var eventStore = Substitute.For<IEventStoreRepository>();
        var service = BuildReferralService(context, producer, eventStore);

        for (var i = 0; i < ReferralSparksRules.MaxInvitedUsers; i++)
        {
            var invited = await TestDataBuilder.SeedUserAsync(context, 9400 + i);
            await service.CreditReferrerForRegistrationAsync(
                invited.Id,
                referrer.Id.ToString(),
                CancellationToken.None);
        }

        var sixth = await TestDataBuilder.SeedUserAsync(context, 9410);
        await service.CreditReferrerForRegistrationAsync(
            sixth.Id,
            referrer.Id.ToString(),
            CancellationToken.None);

        await producer.Received(ReferralSparksRules.MaxInvitedUsers)
            .Produce(Arg.Any<CurrencySparksEvent>(), Arg.Any<CancellationToken>());

        var invitedCount = await context.UserReferenceLinks
            .Where(link => link.UserId == referrer.Id)
            .Select(link => link.CountInvited)
            .SingleAsync();
        invitedCount.Should().Be(ReferralSparksRules.MaxInvitedUsers);

        (await context.ReferralInvites.CountAsync(invite => invite.ReferrerUserId == referrer.Id))
            .Should()
            .Be(ReferralSparksRules.MaxInvitedUsers);
    }

    private static CurrencyCreditedSparksByLinkService BuildReferralService(
        LooksRatingApi.LooksRatingDbContext context,
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
}
