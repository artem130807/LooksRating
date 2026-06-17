using LooksRatingApi.CQRS.UserSessions.Command.EnsureUserSession;
using LooksRatingApi.CQRS.UserSessions.Command.LinkUserSession;
using LooksRatingApi.CQRS.UserSessions.Command.UpdateUserSessionState;
using LooksRatingApi.CQRS.UserSessions.Query.GetUserSession;
using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/user-sessions")]
    public class UserSessionsController : ControllerBase
    {
        private readonly ISender _sender;

        public UserSessionsController(ISender sender)
        {
            _sender = sender;
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("{telegramId:long}")]
        public async Task<IActionResult> Get(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserSessionQuery(telegramId), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpPost("ensure")]
        public async Task<IActionResult> Ensure(
            [FromBody] EnsureUserSessionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new EnsureUserSessionCommand(request.TelegramId, request.InitialState),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPut("state")]
        public async Task<IActionResult> UpdateState(
            [FromBody] UpdateUserSessionStateRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new UpdateUserSessionStateCommand(request.TelegramId, request.State),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == "Сессия не найдена")
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPut("link")]
        public async Task<IActionResult> Link(
            [FromBody] LinkUserSessionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new LinkUserSessionCommand(request.TelegramId, request.UserId),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error is "Сессия не найдена" or "Пользователь не найден")
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
    }

    public sealed class EnsureUserSessionRequest
    {
        public long TelegramId { get; set; }
        public BotSessionState? InitialState { get; set; }
    }

    public sealed class LinkUserSessionRequest
    {
        public long TelegramId { get; set; }
        public Guid UserId { get; set; }
    }
}
