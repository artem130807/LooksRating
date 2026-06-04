using LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket;
using LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketById;
using LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketsByCity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/user-tickets")]
    public class UserTicketsController : ControllerBase
    {
        private readonly ISender _sender;

        public UserTicketsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserTicketRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateUserTicketCommand(
                request.ReporterTelegramId,
                request.PhotoProfileId,
                request.Description);

            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error is CreateUserTicketErrors.ReporterNotFound
                    or CreateUserTicketErrors.PhotoProfileNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error is CreateUserTicketErrors.TicketAlreadyExists
                    or CreateUserTicketErrors.SelfComplaintIsNotAllowed)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserTicketByIdQuery(id), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }

        [HttpPost("by-city")]
        public async Task<IActionResult> GetByCity(
            [FromBody] GetUserTicketsByCityRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserTicketsByCityQuery(request.City), cancellationToken);
            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }
    }
}
