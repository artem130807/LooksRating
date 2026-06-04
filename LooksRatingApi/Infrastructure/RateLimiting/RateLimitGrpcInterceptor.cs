using Grpc.Core;
using Grpc.Core.Interceptors;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitGrpcInterceptor : Interceptor
    {
        private readonly RateLimitGuard _rateLimitGuard;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RateLimitGrpcInterceptor(
            RateLimitGuard rateLimitGuard,
            IHttpContextAccessor httpContextAccessor)
        {
            _rateLimitGuard = rateLimitGuard;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var httpContext = context.GetHttpContext() ?? _httpContextAccessor.HttpContext;
            var remoteIp = RateLimitGrpcPeerAddress.TryResolve(context);

            var decision = await _rateLimitGuard.EvaluateGrpcAsync(
                httpContext,
                remoteIp,
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
