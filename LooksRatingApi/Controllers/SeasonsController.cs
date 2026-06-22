using LooksRatingApi.CQRS.Seasons.Query.GetCurrentSeason;
using LooksRatingApi.CQRS.Seasons.Query.GetSeasonById;
using LooksRatingApi.CQRS.Seasons.Query.GetSeasonsByChapter;
using LooksRatingApi.CQRS.Seasons.Query.GetPendingSeasonRolloverNotifications;
using LooksRatingApi.CQRS.Seasons.Command.AckSeasonRolloverNotification;
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

        [HttpGet("rollover-notifications/pending")]
        public async Task<IActionResult> GetPendingSeasonRolloverNotifications(
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetPendingSeasonRolloverNotificationsQuery(limit), cancellationToken);
            return Ok(result);
        }

        [HttpPost("rollover-notifications/ack")]
        public async Task<IActionResult> AckSeasonRolloverNotification(
            [FromBody] AckSeasonRolloverNotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new AckSeasonRolloverNotificationCommand(
                    request.EventId,
                    request.RecipientTelegramIds),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error switch
                {
                    "SeasonRolloverNotificationNotFound" => NotFound(new { error = result.Error }),
                    _ => BadRequest(new { error = result.Error })
                };
            }

            return Ok(new { status = result.Value });
        }
    }
}
