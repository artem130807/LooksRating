using LooksRatingApi.CQRS.UserReferenceLink.Command.CreateUserReferenceLink;
using LooksRatingApi.CQRS.UserReferenceLink.Query.GetUserReferenceLink;
using LooksRatingApi.Infrastructure.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/user-reference-links")]
    public sealed class UserReferenceLinkController : ControllerBase
    {
        private readonly ISender _sender;

        public UserReferenceLinkController(ISender sender)
        {
            _sender = sender;
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("{telegramId:long}")]
        public async Task<IActionResult> Get(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserReferenceLinkQuery(telegramId), cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            return Ok(new { link = result.Value });
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPost("{telegramId:long}")]
        public async Task<IActionResult> Create(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new CreateUserReferenceLinkCommand(telegramId), cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            return Ok(new { link = result.Value });
        }
    }
}
