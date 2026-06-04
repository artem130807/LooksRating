using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Contracts.ProductContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using MediatR;

namespace LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder
{
    public sealed class CreatePaymentOrderHandler
        : IRequestHandler<CreatePaymentOrderCommand, Result<CreatePaymentOrderResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPaymentOrderRepository _paymentOrderRepository;

        public CreatePaymentOrderHandler(
            IUserRepository userRepository,
            IProductRepository productRepository,
            IPaymentOrderRepository paymentOrderRepository)
        {
            _userRepository = userRepository;
            _productRepository = productRepository;
            _paymentOrderRepository = paymentOrderRepository;
        }

        public async Task<Result<CreatePaymentOrderResponse>> Handle(
            CreatePaymentOrderCommand request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return Result.Failure<CreatePaymentOrderResponse>(CreatePaymentOrderErrors.InvalidTelegramId);
            }

            if (request.ProductCode <= 0)
            {
                return Result.Failure<CreatePaymentOrderResponse>(CreatePaymentOrderErrors.InvalidProductCode);
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<CreatePaymentOrderResponse>(CreatePaymentOrderErrors.UserNotFound);
            }

            if (user.Status == VipStatus.Availlable)
            {
                return Result.Failure<CreatePaymentOrderResponse>(CreatePaymentOrderErrors.VipAlreadyActive);
            }

            var product = await _productRepository.GetByCodeAsync(request.ProductCode, cancellationToken);
            if (product is null)
            {
                return Result.Failure<CreatePaymentOrderResponse>(CreatePaymentOrderErrors.ProductNotFound);
            }

            var payload = $"vip:{request.TelegramId}:{Guid.NewGuid():N}";
            var orderResult = PaymentOrder.Create(
                user.Id,
                product.Id,
                payload,
                product.CountStars,
                product.Currency);
            if (orderResult.IsFailure)
            {
                return Result.Failure<CreatePaymentOrderResponse>(orderResult.Error);
            }

            await _paymentOrderRepository.CreateAsync(orderResult.Value, cancellationToken);

            return Result.Success(new CreatePaymentOrderResponse
            {
                OrderId = orderResult.Value.Id,
                Payload = payload,
                ProductCode = product.ProductCode,
                ProductName = product.Name,
                AmountStars = product.CountStars,
                Currency = product.Currency,
            });
        }
    }
}
