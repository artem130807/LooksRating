using LooksRatingApi.CQRS.Payments.Command.ConfirmPaymentOrder;
using LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly ISender _sender;

        public PaymentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreatePaymentOrderRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new CreatePaymentOrderCommand(request.TelegramId, request.ProductCode),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == CreatePaymentOrderErrors.UserNotFound
                    || result.Error == CreatePaymentOrderErrors.ProductNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error == CreatePaymentOrderErrors.VipAlreadyActive)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpPost("orders/confirm")]
        public async Task<IActionResult> ConfirmOrder(
            [FromBody] ConfirmPaymentOrderRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new ConfirmPaymentOrderCommand(
                    request.TelegramId,
                    request.Payload,
                    request.TelegramPaymentChargeId,
                    request.ProviderPaymentChargeId),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == ConfirmPaymentOrderErrors.OrderNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
    }
}
