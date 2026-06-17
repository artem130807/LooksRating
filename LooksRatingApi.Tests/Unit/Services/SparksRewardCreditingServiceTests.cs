using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Contracts.ProductContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class SparksRewardCreditingServiceTests
{
    [Fact]
    public async Task CreditAsync_WhenRecipientsEmpty_ReturnsZeros()
    {
        var service = CreateService(CreateContext());

        var result = await service.CreditAsync(
            Array.Empty<SparksRewardRecipient>(),
            productCode: 1001,
            rewardSource: "test",
            CancellationToken.None);

        result.Should().Be(new SparksRewardCreditingResult(0, 0, 0, 0));
    }

    [Fact]
    public async Task CreditAsync_WhenProductMissing_CountsAsFailed()
    {
        var context = CreateContext();
        var service = CreateService(context);
        var recipients = new[]
        {
            new SparksRewardRecipient(101, 1, 800m, "season-sparks:test:1:101:abcd1234"),
        };

        var result = await service.CreditAsync(recipients, 1001, "season-top", CancellationToken.None);

        result.Should().Be(new SparksRewardCreditingResult(0, 0, 0, 1));
    }

    [Fact]
    public async Task CreditAsync_CreditsWalletAndPersistsGrantMarker()
    {
        var context = CreateContext();
        var product = await TestDataBuilder.SeedVipProductAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 701);
        await SeedSparksWalletAsync(context, user.Id);

        var sparksService = Substitute.For<ICurrencySparksService>();
        var service = CreateService(context, sparksService);
        var payload = "season-sparks:season:1:701:abcd1234";
        var recipients = new[]
        {
            new SparksRewardRecipient(701, 1, 800m, payload),
        };

        var result = await service.CreditAsync(recipients, product.ProductCode, "season-top", CancellationToken.None);

        result.Should().Be(new SparksRewardCreditingResult(1, 0, 0, 0));
        await sparksService.Received(1).Credited(user.Id, 800m, Arg.Any<CancellationToken>());

        var grant = await context.PaymentOrders.SingleAsync();
        grant.UserId.Should().Be(user.Id);
        grant.Payload.Should().Be(payload);
        grant.TelegramPaymentChargeId.Should().Be($"sparks:{payload}");
    }

    [Fact]
    public async Task CreditAsync_WhenPayloadAlreadyPaid_SkipsRecipient()
    {
        var context = CreateContext();
        var product = await TestDataBuilder.SeedVipProductAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 702);
        await SeedSparksWalletAsync(context, user.Id);

        var payload = "season-sparks:season:1:702:abcd1234";
        var existingGrant = PaymentOrder.CreateSparksRewardGrant(user.Id, product.Id, payload).Value;
        context.PaymentOrders.Add(existingGrant);
        await context.SaveChangesAsync();

        var sparksService = Substitute.For<ICurrencySparksService>();
        var service = CreateService(context, sparksService);
        var recipients = new[]
        {
            new SparksRewardRecipient(702, 1, 800m, payload),
        };

        var result = await service.CreditAsync(recipients, product.ProductCode, "season-top", CancellationToken.None);

        result.Should().Be(new SparksRewardCreditingResult(0, 1, 0, 0));
        await sparksService.DidNotReceive().Credited(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreditAsync_WhenWalletMissing_CountsAsNotFound()
    {
        var context = CreateContext();
        await TestDataBuilder.SeedVipProductAsync(context);
        await TestDataBuilder.SeedUserAsync(context, 703);

        var service = CreateService(context);
        var recipients = new[]
        {
            new SparksRewardRecipient(703, 1, 800m, "season-sparks:season:1:703:abcd1234"),
        };

        var result = await service.CreditAsync(recipients, 1001, "season-top", CancellationToken.None);

        result.Should().Be(new SparksRewardCreditingResult(0, 0, 1, 0));
    }

    private static SparksRewardCreditingService CreateService(
        LooksRatingDbContext context,
        ICurrencySparksService? sparksService = null)
    {
        return new SparksRewardCreditingService(
            sparksService ?? Substitute.For<ICurrencySparksService>(),
            new PaymentOrderRepository(context),
            new ProductRepository(context),
            context,
            NullLogger<SparksRewardCreditingService>.Instance);
    }

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }

    private static async Task SeedSparksWalletAsync(LooksRatingDbContext context, Guid userId)
    {
        var wallet = LooksRatingApi.Models.SparksWallet.Create(userId, 0m).Value;
        context.SparksLedgers.Add(wallet);
        await context.SaveChangesAsync();
    }
}
