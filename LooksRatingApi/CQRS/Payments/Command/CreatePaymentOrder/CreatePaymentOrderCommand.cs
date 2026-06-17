using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder
{
    public sealed record CreatePaymentOrderCommand(long TelegramId, int ProductCode)
        : IRequest<Result<CreatePaymentOrderResponse>>;
}
