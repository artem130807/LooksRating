using LooksRatingApi.CQRS.ListSeasons.Query.GetLatestListSeason;
using LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasonById;
using LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasons;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/list-seasons")]
    public class ListSeasonsController : ControllerBase
    {
        private readonly ISender _sender;

        public ListSeasonsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool includeSeasons = false,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetListSeasonsQuery(includeSeasons), cancellationToken);
            return Ok(result.Value);
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest(
            [FromQuery] bool includeSeasons = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetLatestListSeasonQuery(includeSeasons), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetListSeasonByIdQuery(id), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }
    }
}
