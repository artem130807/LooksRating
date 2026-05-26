using LooksRatingApi.CQRS.Seasons.Query.GetCurrentSeason;
using LooksRatingApi.CQRS.Seasons.Query.GetSeasonById;
using LooksRatingApi.CQRS.Seasons.Query.GetSeasonsByChapter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/seasons")]
    public class SeasonsController : ControllerBase
    {
        private readonly ISender _sender;

        public SeasonsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent(
            [FromQuery] Guid? listSeasonsId,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetCurrentSeasonQuery(listSeasonsId), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            [FromQuery] bool includeChapter = false,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetSeasonByIdQuery(id, includeChapter), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }

        [HttpGet("by-chapter/{listSeasonsId:guid}")]
        public async Task<IActionResult> GetByChapter(
            Guid listSeasonsId,
            [FromQuery] bool includeClosed = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetSeasonsByChapterQuery(listSeasonsId, includeClosed),
                cancellationToken);

            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }
    }
}
