using LooksRatingApi.Contracts.AdminModeration;
using LooksRatingApi.Infrastructure.RateLimiting;
using LooksRatingApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/internal/moderation")]
    public sealed class AdminModerationController : ControllerBase
    {
        private readonly AdminTicketModerationService _moderationService;
        private readonly ILogger<AdminModerationController> _logger;

        public AdminModerationController(
            AdminTicketModerationService moderationService,
            ILogger<AdminModerationController> logger)
        {
            _moderationService = moderationService;
            _logger = logger;
        }

        [HttpGet("cities")]
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        public async Task<ActionResult<ModerationCitiesResponse>> GetCities(CancellationToken cancellationToken)
        {
            try
            {
                var cities = await _moderationService.ListModerationCitiesAsync(cancellationToken);
                return Ok(new ModerationCitiesResponse { Cities = cities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListModerationCities http failed: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Не удалось загрузить список городов" });
            }
        }

        [HttpGet("cities/{city}/tickets")]
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        public Task<ActionResult<ModerationTicketsByCityResponse>> ListTicketsByCity(
            string city,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default) =>
            ListTicketsByCityInternal(city, offset, limit, cancellationToken);

        [HttpGet("tickets-by-city")]
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        public Task<ActionResult<ModerationTicketsByCityResponse>> ListTicketsByCityQuery(
            [FromQuery] string city,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default) =>
            ListTicketsByCityInternal(city, offset, limit, cancellationToken);

        private async Task<ActionResult<ModerationTicketsByCityResponse>> ListTicketsByCityInternal(
            string city,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            var result = await _moderationService.ListTicketsByCityAsync(city, offset, limit, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpGet("tickets/{ticketId}")]
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        public async Task<ActionResult<ModerationTicketDetailDto>> GetTicketDetail(
            string ticketId,
            CancellationToken cancellationToken)
        {
            var result = await _moderationService.GetTicketDetailAsync(ticketId, cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpGet("tickets-by-city/count")]
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        public async Task<ActionResult<ModerationTicketCountResponse>> CountTicketsByCity(
            [FromQuery] string city,
            CancellationToken cancellationToken = default)
        {
            var result = await _moderationService.CountQueuedTicketsAsync(city, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpGet("tickets-by-city/queue")]
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        public async Task<ActionResult<ModerationQueuedTicketResponse>> GetQueuedTicket(
            [FromQuery] string city,
            [FromQuery] int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var result = await _moderationService.GetQueuedTicketAsync(city, offset, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpPost("tickets/{ticketId}/dismiss")]
        [RateLimitPolicy(RateLimitPolicies.Writes)]
        public async Task<IActionResult> DismissTicket(
            string ticketId,
            [FromBody] ModerationActionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _moderationService.DismissTicketAsync(
                ticketId,
                request.AdminTelegramId,
                cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation(
                "DismissTicket http ok ticket={TicketId} admin={AdminTelegramId}",
                ticketId,
                request.AdminTelegramId);

            return Ok(new { success = true });
        }

        [HttpPost("tickets/{ticketId}/delete-profile")]
        [RateLimitPolicy(RateLimitPolicies.Writes)]
        public async Task<IActionResult> DeleteReportedProfile(
            string ticketId,
            [FromBody] ModerationActionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _moderationService.DeleteReportedProfileAsync(
                ticketId,
                request.AdminTelegramId,
                cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation(
                "DeleteReportedProfile http ok ticket={TicketId} admin={AdminTelegramId}",
                ticketId,
                request.AdminTelegramId);

            return Ok(new { success = true });
        }

        [HttpPost("tickets/{ticketId}/delete-account")]
        [RateLimitPolicy(RateLimitPolicies.Writes)]
        public async Task<IActionResult> DeleteReportedUserAccount(
            string ticketId,
            [FromBody] ModerationActionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _moderationService.DeleteReportedUserAccountAsync(
                ticketId,
                request.AdminTelegramId,
                cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation(
                "DeleteReportedUserAccount http ok ticket={TicketId} admin={AdminTelegramId}",
                ticketId,
                request.AdminTelegramId);

            return Ok(new { success = true });
        }
    }
}
