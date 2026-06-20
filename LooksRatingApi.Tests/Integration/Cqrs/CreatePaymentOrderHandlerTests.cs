using LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder;
using LooksRatingApi.Infrastructure.Startup;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Tests.Integration.Cqrs;

[Collection(IntegrationCollection.Name)]
public sealed class CreatePaymentOrderHandlerTests
{
    private readonly PostgresFixture _postgres;

    public CreatePaymentOrderHandlerTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [SkippableFact]
    public async Task Handle_ReturnsConfiguredVipPrice()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var user = await TestDataBuilder.SeedUserAsync(context, 8901);
        await TestDataBuilder.SeedVipProductAsync(context);

        var handler = new CreatePaymentOrderHandler(
            new UserRepository(context),
            new ProductRepository(context),
            new PaymentOrderRepository(context));

        var result = await handler.Handle(
            new CreatePaymentOrderCommand(user.TelegramId, VipTopRules.VipProductCode),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AmountStars.Should().Be(140);
        result.Value.ProductName.Should().Be("VIP");
    }

    [SkippableFact]
    public async Task VipProductBootstrap_UpdatesLegacyPriceToConfiguredValue()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var legacyProduct = Product.Create(
            "VIP-статус",
            VipTopRules.VipProductCode,
            countStars: 100,
            currency: "XTR",
            vipDays: VipTopRules.DefaultVipDays).Value;
        context.Products.Add(legacyProduct);
        await context.SaveChangesAsync();

        await VipProductBootstrap.EnsureConfiguredAsync(context);

        var product = await context.Products.SingleAsync(p => p.ProductCode == VipTopRules.VipProductCode);
        product.CountStars.Should().Be(140);
        product.IsActive.Should().BeTrue();
    }
}
