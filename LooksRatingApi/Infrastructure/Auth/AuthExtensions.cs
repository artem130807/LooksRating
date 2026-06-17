using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace LooksRatingApi.Infrastructure.Auth
{
    public static class AuthExtensions
    {
        public static IServiceCollection AddApiKeyAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ApiKeyAuthOptions>(configuration.GetSection(ApiKeyAuthOptions.SectionName));

            services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationHandler.SchemeName,
                    _ => { });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }
    }
}
