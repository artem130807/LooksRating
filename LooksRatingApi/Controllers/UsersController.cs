using LooksRatingApi.Cqrs.Users.Command.RegisterUser;
using LooksRatingApi.CQRS.Users.Command.DeleteUserAccount;
using LooksRatingApi.CQRS.Users.Command.UpdateGenderUser;
using LooksRatingApi.CQRS.Users.Command.UpdateUserAge;
using LooksRatingApi.CQRS.Users.Command.UpdateUserCity;
using LooksRatingApi.CQRS.Users.Query.GetUserByTelegramId;
using LooksRatingApi.CQRS.Users.Query.GetUserStats;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Infrastructure.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly ISender _sender;

        public UsersController(ISender sender)
        {
            _sender = sender;
        }

        [RateLimitPolicy(RateLimitPolicies.AccountSensitive)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            var command = new RegisterUserCommand(
                request.TelegramId,
                request.TelegramUsername,
                request.UseTelegramUsernameAsDisplay,
                request.Name,
                request.Link);

            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == RegisterUserErrors.UserAlreadyExists)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("{telegramId:long}")]
        public async Task<IActionResult> GetByTelegramId(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserByTelegramIdQuery(telegramId), cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == "UserNotFound")
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Reads)]
        [HttpGet("{telegramId:long}/stats")]
        public async Task<IActionResult> GetStats(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserStatsQuery(telegramId), cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == SetUserPhotoErrors.UserNotFound)
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPut("gender")]
        public async Task<IActionResult> UpdateGender(
            [FromBody] UpdateGenderUserRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new UpdateGenderUserCommandCommand(request.TelegramId, request.Gender),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == UpdateGenderUserErrors.UserNotFound)
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = result.Value });
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPut("city")]
        public async Task<IActionResult> UpdateCity(
            [FromBody] UpdateUserCityRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new UpdateUserCityCommand(request.TelegramId, request.City),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == UpdateUserCityErrors.UserNotFound)
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = result.Value });
        }

        [RateLimitPolicy(RateLimitPolicies.Writes)]
        [HttpPut("age")]
        public async Task<IActionResult> UpdateAge(
            [FromBody] UpdateUserAgeRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new UpdateUserAgeCommand(request.TelegramId, request.Age),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == UpdateUserAgeErrors.UserNotFound)
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = result.Value });
        }

        [RateLimitPolicy(RateLimitPolicies.AccountSensitive)]
        [HttpDelete("{telegramId:long}")]
        public async Task<IActionResult> DeleteAccount(long telegramId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteUserAccountCommand(telegramId), cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == DeleteUserAccountErrors.UserNotFound)
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return NoContent();
        }
    }
}
