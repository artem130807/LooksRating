using LooksRatingApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitGuard
    {
        private readonly RateLimitingOptions _options;
        private readonly ApiKeyAuthOptions _apiKeyOptions;
        private readonly IRateLimitService _rateLimitService;
        private readonly RateLimitPartitionResolver _partitionResolver;
        private readonly ILogger<RateLimitGuard> _logger;

        public RateLimitGuard(
            IOptions<RateLimitingOptions> options,
            IOptions<ApiKeyAuthOptions> apiKeyOptions,
            IRateLimitService rateLimitService,
            RateLimitPartitionResolver partitionResolver,
            ILogger<RateLimitGuard> logger)
        {
            _options = options.Value;
            _apiKeyOptions = apiKeyOptions.Value;
            _rateLimitService = rateLimitService;
            _partitionResolver = partitionResolver;
            _logger = logger;
        }

        public async Task<RateLimitDecision> EvaluateRestAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return RateLimitDecision.Allowed;
            }

            var endpointPolicies = RateLimitEndpointPolicyResolver.Resolve(context);
            var policyNames = RateLimitEndpointPolicyResolver.BuildRestPolicyNames(endpointPolicies);
            return await EvaluatePoliciesAsync(context, policyNames, cancellationToken);
        }

        public async Task<RateLimitDecision> EvaluateGrpcAsync(
            HttpContext? httpContext,
            string? remoteIp,
            string? providedApiKey,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return RateLimitDecision.Allowed;
            }

            if (httpContext is null && string.IsNullOrWhiteSpace(remoteIp))
            {
                _logger.LogWarning("gRPC rate limit skipped: no HttpContext and no peer IP");
                return _options.FailOpen
                    ? RateLimitDecision.Allowed
                    : new RateLimitDecision(false, RateLimitPolicies.Grpc, 60);
            }

            var policyNames = ResolveGrpcPolicyNames(httpContext, providedApiKey);
            if (httpContext is not null)
            {
                return await EvaluatePoliciesAsync(httpContext, policyNames, cancellationToken);
            }

            return await EvaluatePoliciesForGrpcPeerAsync(remoteIp!, policyNames, cancellationToken);
        }

        private IReadOnlyList<string> ResolveGrpcPolicyNames(HttpContext? httpContext, string? providedApiKey)
        {
            if (!IsTrustedServiceApiKey(providedApiKey)
                && !IsTrustedServiceApiKeyFromHttp(httpContext))
            {
                return RateLimitEndpointPolicyResolver.BuildGrpcPolicyNames();
            }

            return [RateLimitPolicies.Global];
        }

        private bool IsTrustedServiceApiKey(string? providedApiKey)
        {
            if (string.IsNullOrWhiteSpace(providedApiKey)
                || string.IsNullOrWhiteSpace(_apiKeyOptions.ApiKey))
            {
                return false;
            }

            return string.Equals(
                providedApiKey,
                _apiKeyOptions.ApiKey,
                StringComparison.Ordinal);
        }

        private bool IsTrustedServiceApiKeyFromHttp(HttpContext? httpContext)
        {
            if (httpContext is null)
            {
                return false;
            }

            if (!httpContext.Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var providedKey))
            {
                return false;
            }

            return IsTrustedServiceApiKey(providedKey.ToString());
        }

        private async Task<RateLimitDecision> EvaluatePoliciesAsync(
            HttpContext context,
            IReadOnlyList<string> policyNames,
            CancellationToken cancellationToken)
        {
            var globalPartition = _partitionResolver.ResolveGlobalPartitionKey(context);
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            string? telegramPartition = null;

            if (policyNames.Any(RateLimitPolicyRegistry.UsesTelegramPartition))
            {
                telegramPartition = await _partitionResolver.ResolveTelegramOrFallbackPartitionAsync(
                    context,
                    globalPartition);
            }

            return await EvaluatePolicyListAsync(
                policyNames,
                globalPartition,
                telegramPartition,
                remoteIp,
                context.Request.Path.Value,
                cancellationToken);
        }

        private async Task<RateLimitDecision> EvaluatePoliciesForGrpcPeerAsync(
            string remoteIp,
            IReadOnlyList<string> policyNames,
            CancellationToken cancellationToken)
        {
            var grpcPartition = _partitionResolver.ResolveGrpcPartitionKey(remoteIp);
            return await EvaluatePolicyListAsync(
                policyNames,
                globalPartition: grpcPartition,
                telegramPartition: null,
                remoteIp: remoteIp,
                requestPath: "grpc",
                cancellationToken);
        }

        private async Task<RateLimitDecision> EvaluatePolicyListAsync(
            IReadOnlyList<string> policyNames,
            string globalPartition,
            string? telegramPartition,
            string? remoteIp,
            string? requestPath,
            CancellationToken cancellationToken)
        {
            var retryAfterSeconds = 0;
            string? rejectedPolicy = null;

            foreach (var policyName in policyNames)
            {
                var partitionKey = ResolvePartitionKey(
                    policyName,
                    globalPartition,
                    telegramPartition,
                    remoteIp);

                var acquireResult = await _rateLimitService.TryAcquireAsync(
                    policyName,
                    partitionKey,
                    cancellationToken);

                if (acquireResult.IsAcquired)
                {
                    continue;
                }

                rejectedPolicy = policyName;
                retryAfterSeconds = Math.Max(retryAfterSeconds, acquireResult.RetryAfterSeconds);
                break;
            }

            if (rejectedPolicy is null)
            {
                return RateLimitDecision.Allowed;
            }

            _logger.LogWarning(
                "Rate limit exceeded. Policy={Policy}, Path={Path}, RetryAfter={RetryAfterSeconds}",
                rejectedPolicy,
                requestPath,
                retryAfterSeconds);

            return new RateLimitDecision(false, rejectedPolicy, retryAfterSeconds);
        }

        private string ResolvePartitionKey(
            string policyName,
            string globalPartition,
            string? telegramPartition,
            string? remoteIp)
        {
            if (string.Equals(policyName, RateLimitPolicies.Global, StringComparison.OrdinalIgnoreCase))
            {
                return globalPartition;
            }

            if (string.Equals(policyName, RateLimitPolicies.Grpc, StringComparison.OrdinalIgnoreCase))
            {
                return _partitionResolver.ResolveGrpcPartitionKey(remoteIp);
            }

            return telegramPartition ?? $"fallback:{globalPartition}";
        }
    }
}
