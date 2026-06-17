using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.CQRS.Reviews.Command.AckMilestoneNotification;
using LooksRatingApi.CQRS.Reviews.Query.GetMilestoneReviewers;
using LooksRatingApi.CQRS.Reviews.Query.GetPendingMilestoneNotifications;
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

        [HttpGet("milestone-notifications/pending")]
        public async Task<IActionResult> GetPendingMilestoneNotifications(
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetPendingMilestoneNotificationsQuery(limit), cancellationToken);
            return Ok(result);
        }

        [HttpPost("milestone-notifications/{id:guid}/ack")]
        public async Task<IActionResult> AckMilestoneNotification(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new AckMilestoneNotificationCommand(id), cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            return Ok(new { status = result.Value });
        }

        [HttpGet("milestone-notifications/{id:guid}/reviewers")]
        public async Task<IActionResult> GetMilestoneReviewers(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetMilestoneReviewersQuery(id), cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
    }
}
