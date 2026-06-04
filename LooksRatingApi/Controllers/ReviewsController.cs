using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.Infrastructure.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly ISender _sender;

        public ReviewsController(ISender sender)
        {
            _sender = sender;
        }

        [RateLimitPolicy(RateLimitPolicies.Rating)]
        [HttpPost("create_review")]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateReviewCommand(
                request.ReviewerTelegramId,
                request.PhotoProfileId,
                request.Rating);

            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == CreateReviewErrors.ReviewerNotFound
                    || result.Error == CreateReviewErrors.PhotoProfileNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error == CreateReviewErrors.ReviewAlreadyExists
                    || result.Error == CreateReviewErrors.SelfReviewIsNotAllowed)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
    }
}
