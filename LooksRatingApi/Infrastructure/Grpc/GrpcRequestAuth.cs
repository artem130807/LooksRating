using Grpc.Core;
using LooksRatingApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.Grpc
{
    internal static class GrpcRequestAuth
    {
        public static void EnsureApiKey(ServerCallContext context, IOptions<ApiKeyAuthOptions> options)
        {
            var authOptions = options.Value;
            if (!authOptions.RequireApiKey || string.IsNullOrWhiteSpace(authOptions.ApiKey))
            {
                return;
            }

            var headerName = authOptions.HeaderName.ToLowerInvariant();
            var provided = context.RequestHeaders.FirstOrDefault(
                entry => string.Equals(entry.Key, headerName, StringComparison.OrdinalIgnoreCase));

            if (provided is null || !string.Equals(provided.Value, authOptions.ApiKey, StringComparison.Ordinal))
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Неверный API key"));
            }
        }
    }
}
