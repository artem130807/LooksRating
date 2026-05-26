using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.Auth
{
    public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ApiKey";

        private readonly ApiKeyAuthOptions _apiKeyOptions;
        private readonly IHostEnvironment _hostEnvironment;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<ApiKeyAuthOptions> apiKeyOptions,
            IHostEnvironment hostEnvironment)
            : base(options, logger, encoder)
        {
            _apiKeyOptions = apiKeyOptions.Value;
            _hostEnvironment = hostEnvironment;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (IsPublicPath(Request.Path))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!_apiKeyOptions.RequireApiKey || string.IsNullOrWhiteSpace(_apiKeyOptions.ApiKey))
            {
                return Task.FromResult(AuthenticateResult.Success(CreateTicket("development")));
            }

            if (!Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var providedKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API key is missing"));
            }

            if (!string.Equals(providedKey.ToString(), _apiKeyOptions.ApiKey, StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.Fail("API key is invalid"));
            }

            return Task.FromResult(AuthenticateResult.Success(CreateTicket("api-client")));
        }

        private bool IsPublicPath(PathString path)
        {
            if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return _hostEnvironment.IsDevelopment()
                && path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
        }

        private AuthenticationTicket CreateTicket(string name)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, name)],
                SchemeName);

            return new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        }
    }
}
