using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.CQRS.Payments.Command.ConfirmPaymentOrder
{
    public sealed class ConfirmPaymentOrderHandler
        : IRequestHandler<ConfirmPaymentOrderCommand, Result<ConfirmPaymentOrderResponse>>
    {
        private readonly LooksRatingDbContext _context;
        private readonly IPaymentOrderRepository _paymentOrderRepository;
        private readonly IUserRepository _userRepository;

        public ConfirmPaymentOrderHandler(
            LooksRatingDbContext context,
            IPaymentOrderRepository paymentOrderRepository,
            IUserRepository userRepository)
        {
            _context = context;
            _paymentOrderRepository = paymentOrderRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<ConfirmPaymentOrderResponse>> Handle(
            ConfirmPaymentOrderCommand request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return Result.Failure<ConfirmPaymentOrderResponse>(ConfirmPaymentOrderErrors.InvalidTelegramId);
            }

            if (string.IsNullOrWhiteSpace(request.Payload))
            {
                return Result.Failure<ConfirmPaymentOrderResponse>(ConfirmPaymentOrderErrors.PayloadIsRequired);
            }

            if (string.IsNullOrWhiteSpace(request.TelegramPaymentChargeId))
            {
                return Result.Failure<ConfirmPaymentOrderResponse>(ConfirmPaymentOrderErrors.TelegramChargeIdIsRequired);
            }

            var order = await _paymentOrderRepository.GetByPayloadAsync(request.Payload, cancellationToken);
            if (order is null)
            {
                return Result.Failure<ConfirmPaymentOrderResponse>(ConfirmPaymentOrderErrors.OrderNotFound);
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<ConfirmPaymentOrderResponse>(ConfirmPaymentOrderErrors.OrderNotFound);
            }

            if (order.UserId != user.Id)
            {
                return Result.Failure<ConfirmPaymentOrderResponse>(ConfirmPaymentOrderErrors.OrderOwnerMismatch);
            }

            var existingByCharge = await _paymentOrderRepository.GetByTelegramChargeIdAsync(
                request.TelegramPaymentChargeId,
                cancellationToken);
            if (existingByCharge is not null && existingByCharge.Id != order.Id)
            {
                return Result.Failure<ConfirmPaymentOrderResponse>(ConfirmPaymentOrderErrors.PaymentAlreadyBound);
            }

            if (order.Status == PaymentOrderStatus.Paid)
            {
                if (user.Status != VipStatus.Availlable)
                {
                    user.UpdateVipStatus();
                    await _paymentOrderRepository.SaveChangesAsync(cancellationToken);
                }

                return Result.Success(new ConfirmPaymentOrderResponse
                {
                    OrderId = order.Id,
                    Paid = true,
                    Message = "Платеж уже подтвержден",
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                order.MarkPaid(request.TelegramPaymentChargeId, request.ProviderPaymentChargeId);
                user.UpdateVipStatus();

                await _paymentOrderRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return Result.Success(new ConfirmPaymentOrderResponse
            {
                OrderId = order.Id,
                Paid = true,
                Message = "Платеж подтвержден, VIP активирован",
            });
        }
    }
}
