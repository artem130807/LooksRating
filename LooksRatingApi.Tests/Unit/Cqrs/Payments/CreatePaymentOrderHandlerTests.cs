using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Contracts.ProductContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Cqrs.Payments;

public sealed class CreatePaymentOrderHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsVipPriceInStars()
    {
        var user = CreateUser(telegramId: 8801);
        var product = Product.Create(
            "VIP-статус",
            VipTopRules.VipProductCode,
            VipTopRules.VipStarsPrice,
            "XTR",
            VipTopRules.DefaultVipDays).Value;

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(user.TelegramId).Returns(user);

        var productRepository = Substitute.For<IProductRepository>();
        productRepository
            .GetByCodeAsync(VipTopRules.VipProductCode, Arg.Any<CancellationToken>())
            .Returns(product);

        PaymentOrder? createdOrder = null;
        var paymentOrderRepository = Substitute.For<IPaymentOrderRepository>();
        paymentOrderRepository
            .CreateAsync(Arg.Any<PaymentOrder>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                createdOrder = callInfo.Arg<PaymentOrder>();
                return Task.CompletedTask;
            });

        var handler = new CreatePaymentOrderHandler(
            userRepository,
            productRepository,
            paymentOrderRepository);

        var result = await handler.Handle(
            new CreatePaymentOrderCommand(user.TelegramId, VipTopRules.VipProductCode),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AmountStars.Should().Be(140);
        result.Value.ProductCode.Should().Be(VipTopRules.VipProductCode);
        result.Value.Currency.Should().Be("XTR");
        createdOrder.Should().NotBeNull();
        createdOrder!.AmountStars.Should().Be(140);
    }

    [Fact]
    public async Task Handle_WhenVipAlreadyActive_ReturnsFailure()
    {
        var user = CreateUser(telegramId: 8802, VipStatus.Availlable);

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(user.TelegramId).Returns(user);

        var handler = new CreatePaymentOrderHandler(
            userRepository,
            Substitute.For<IProductRepository>(),
            Substitute.For<IPaymentOrderRepository>());

        var result = await handler.Handle(
            new CreatePaymentOrderCommand(user.TelegramId, VipTopRules.VipProductCode),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CreatePaymentOrderErrors.VipAlreadyActive);
    }

    private static User CreateUser(long telegramId, VipStatus status = VipStatus.Unavaillable) =>
        new()
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = status,
        };
}
