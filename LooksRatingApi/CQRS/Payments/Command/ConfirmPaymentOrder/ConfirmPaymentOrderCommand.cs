using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Payments.Command.ConfirmPaymentOrder
{
    public sealed record ConfirmPaymentOrderCommand(
        long TelegramId,
        string Payload,
        string TelegramPaymentChargeId,
        string? ProviderPaymentChargeId)
        : IRequest<Result<ConfirmPaymentOrderResponse>>;
}
