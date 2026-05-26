using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.RecomendationSettings;
using LooksRatingApi.CQRS.RecomendationSettings.Command.UpsertRecomendationSettings;
using LooksRatingApi.CQRS.RecomendationSettings.Query.GetRecomendationSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/recomendation-settings")]
    public class RecomendationSettingsController : ControllerBase
    {
        private readonly ISender _sender;

        public RecomendationSettingsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{telegramId:long}")]
        public async Task<IActionResult> Get(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetRecomendationSettingsQuery(telegramId), cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == RecomendationSettingsErrors.UserNotFound)
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpPut]
        public async Task<IActionResult> Upsert(
            [FromBody] UpsertRecomendationSettingsRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new UpsertRecomendationSettingsCommand(
                    request.TelegramId,
                    request.Age,
                    request.Gender,
                    request.City),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == RecomendationSettingsErrors.UserNotFound)
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Успешно" });
        }
    }
}
