
using LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks;
using LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeekById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/the-best-weeks")]
    public class TheBestWeeksController : ControllerBase
    {
        private readonly ISender _sender;

        public TheBestWeeksController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTheBestWeekByIdQuery(id), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }
    }
}
