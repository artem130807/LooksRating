using System.ComponentModel;
using LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhotoBySeason;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetPhotoUserById;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosId;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestVipPhotos;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos;
using LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks;
using LooksRatingApi.Infrastructure.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/photo-users")]
    public class PhotoUsersController : ControllerBase
    {
        private readonly ISender _sender;

        public PhotoUsersController(ISender sender)
        {
            _sender = sender;
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPost("set_photo")]
        public async Task<IActionResult> SetPhoto([FromBody] SetUserPhotoRequest request, CancellationToken cancellationToken)
        {
            var command = new SetUserPhotoCommand(request);
            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == SetUserPhotoErrors.UserNotFound
                    || result.Error == SetUserPhotoErrors.CurrentSeasonNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error == SetUserPhotoErrors.PhotoAlreadyExists)
                {
                    return Conflict(new { error = result.Error });
                }

                if (result.Error == SetUserPhotoErrors.PhotoUploadInProgress)
                {
                    return Conflict(new { error = result.Error });
                }

                if (result.Error == SetUserPhotoErrors.VipPhotoLimitExceeded)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPost("recreate_photo")]
        public async Task<IActionResult> RecreatePhoto(
            [FromBody] RecreateUserPhotoRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RecreateUserPhotoCommand(request), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error is SetUserPhotoErrors.UserNotFound
                    or SetUserPhotoErrors.CurrentSeasonNotFound
                    or RecreateUserPhotoErrors.PhotoNotFound
                    or RecreateUserPhotoErrors.TargetPhotoNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPost("recreate_all_photos")]
        public async Task<IActionResult> RecreateAllPhotos(
            [FromBody] RecreateAllUserPhotosRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RecreateAllUserPhotosCommand(request), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error is SetUserPhotoErrors.UserNotFound
                    or SetUserPhotoErrors.CurrentSeasonNotFound
                    or RecreateUserPhotoErrors.PhotoNotFound
                    or RecreateUserPhotoErrors.TargetPhotoNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetPhotoUserByIdQuery(id), cancellationToken);
            if (result.IsFailure)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("my/{telegramId:long}")]
        public async Task<IActionResult> GetMyPhoto(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetMyPhotoQuery(telegramId), cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error is SetUserPhotoErrors.UserNotFound or "PhotoNotFound")
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("my/{telegramId:long}/seasons/{seasonId:guid}")]
        public async Task<IActionResult> GetMyPhotoBySeason(
            long telegramId,
            Guid seasonId,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new GetMyPhotoBySeasonQuery(telegramId, seasonId),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error is SetUserPhotoErrors.UserNotFound
                    or "PhotoNotFound"
                    or "SeasonNotFound")
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.GetNextPhoto)]
        [HttpPost("get_next_photo")]
        public async Task<IActionResult> GetNextPhoto(
            [FromBody] GetNextPhotoRequest request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return BadRequest(new { error = SetUserPhotoErrors.TelegramIdIsRequired });
            }

            var query = new GetUserPhotosQuery(request.TelegramId);
            var result = await _sender.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == SetUserPhotoErrors.UserNotFound)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error is GetUserPhotosErrors.NoPhotosAvailable
                    or GetUserPhotosErrors.RecommendationSettingsIncomplete
                    or CreateReviewErrors.SelfReviewIsNotAllowed)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }
            return Ok(result.Value);
        }
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpPost("get_top_photos")]
        public async Task<IActionResult> GetTopPhotos([FromBody] GetTopUserPhotosRequest request, CancellationToken cancellationToken)
        {
            var query = new GetTopUserPhotosQuery(
                request.TelegramId,
                request.GenderEnum,
                request.Age,
                request.SeasonId,
                request.Page,
                request.PageSize);

            var result = await _sender.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error is SetUserPhotoErrors.UserNotFound or "SeasonNotFound")
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        [HttpGet("get_theBestWeek_photosId")]
        public async Task<IActionResult> GetTheBestWeekPhotosId(CancellationToken cancellationToken)
        {
            var query = new GetTheBestWeekPhotosIdQuery();
            var result = await _sender.Send(query, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }
            return Ok(result.Value);
        }
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("get_thebestWeek_photosNow")]
        public async Task<IActionResult> GetTheBestWeekPhotosNow([FromQuery] GetTheBestWeekPhotosNowRequest request, CancellationToken cancellationToken)
        {
            var query = new GetTheBestWeekPhotosNowQuery(request.TelegramId, 
            request.GenderEnum, 
            request.Age);
            var result = await _sender.Send(query, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }
            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("get_thebestvip_photos")]
        public async Task<IActionResult> GetTheBestVipPhotos(
            [FromQuery] GetTheBestVipPhotosRequest request,
            CancellationToken cancellationToken)
        {
            var query = new GetTheBestVipPhotosQuery(
                request.TelegramId,
                request.GenderEnum,
                request.Age);
            var result = await _sender.Send(query, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("get_thebestWeek_photos")]
        public async Task<IActionResult> GetTheBestWeekPhotos([FromQuery] GetTheBestWeeksRequest request, CancellationToken cancellationToken)
        {
            var query = new GetTheBestWeeksQuery(request.TelegramId, request.GenderEnum, request.Age);
            var result = await _sender.Send(query, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == SetUserPhotoErrors.UserNotFound)
                {
                    return NotFound(new { error = result.Error });
                }
                return BadRequest(new { error = result.Error });
            }
            return Ok(result.Value);
        }
    }
}
