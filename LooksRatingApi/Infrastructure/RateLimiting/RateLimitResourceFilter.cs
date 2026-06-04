using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitResourceFilter : IAsyncResourceFilter
    {
        private readonly RateLimitGuard _rateLimitGuard;

        public RateLimitResourceFilter(RateLimitGuard rateLimitGuard)
        {
            _rateLimitGuard = rateLimitGuard;
        }

        public async Task OnResourceExecutionAsync(
            ResourceExecutingContext context,
            ResourceExecutionDelegate next)
        {
            var decision = await _rateLimitGuard.EvaluateRestAsync(
                context.HttpContext,
                context.HttpContext.RequestAborted);

            if (!decision.IsAllowed)
            {
                if (decision.RetryAfterSeconds > 0)
                {
                    context.HttpContext.Response.Headers.RetryAfter = decision.RetryAfterSeconds.ToString();
                }

                context.Result = CreateTooManyRequestsResult(decision);
                return;
            }

            await next();
        }

        private static ObjectResult CreateTooManyRequestsResult(RateLimitDecision decision)
        {
            return new ObjectResult(new
            {
                error = "TooManyRequests",
                policy = decision.Policy,
                retryAfterSeconds = decision.RetryAfterSeconds,
            })
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
        }
    }
}
