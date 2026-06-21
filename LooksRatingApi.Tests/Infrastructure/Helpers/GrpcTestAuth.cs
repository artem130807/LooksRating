using LooksRatingApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Tests.Infrastructure.Helpers;

internal static class GrpcTestAuth
{
    public static IOptions<ApiKeyAuthOptions> Disabled() =>
        Options.Create(new ApiKeyAuthOptions { RequireApiKey = false });
}
