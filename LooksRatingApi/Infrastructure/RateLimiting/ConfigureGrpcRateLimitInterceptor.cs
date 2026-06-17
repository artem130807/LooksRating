using Grpc.AspNetCore.Server;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    internal sealed class ConfigureGrpcRateLimitInterceptor : IConfigureOptions<GrpcServiceOptions>
    {
        public void Configure(GrpcServiceOptions options)
        {
            options.Interceptors.Add<RateLimitGrpcInterceptor>();
        }
    }
}
