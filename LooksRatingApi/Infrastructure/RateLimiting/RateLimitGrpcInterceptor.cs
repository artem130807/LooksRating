using Grpc.Core;
using Grpc.Core.Interceptors;
using LooksRatingApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitGrpcInterceptor : Interceptor
    {
        private readonly RateLimitGuard _rateLimitGuard;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApiKeyAuthOptions _apiKeyOptions;

        public RateLimitGrpcInterceptor(
            RateLimitGuard rateLimitGuard,
            IHttpContextAccessor httpContextAccessor,
            IOptions<ApiKeyAuthOptions> apiKeyOptions)
        {
            _rateLimitGuard = rateLimitGuard;
            _httpContextAccessor = httpContextAccessor;
            _apiKeyOptions = apiKeyOptions.Value;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var httpContext = context.GetHttpContext() ?? _httpContextAccessor.HttpContext;
            var remoteIp = RateLimitGrpcPeerAddress.TryResolve(context);
            var providedApiKey = context.RequestHeaders.FirstOrDefault(
                entry => string.Equals(entry.Key, _apiKeyOptions.HeaderName, StringComparison.OrdinalIgnoreCase))?.Value;

            var decision = await _rateLimitGuard.EvaluateGrpcAsync(
                httpContext,
                remoteIp,
                providedApiKey,
                context.CancellationToken);

            if (!decision.IsAllowed)
            {
                var metadata = new Metadata();
                if (decision.RetryAfterSeconds > 0)
                {
                    metadata.Add("retry-after", decision.RetryAfterSeconds.ToString());
                }

                throw new RpcException(
                    new Status(StatusCode.ResourceExhausted, $"Rate limit exceeded: {decision.Policy}"),
                    metadata);
            }

            return await continuation(request, context);
        }
    }
}
