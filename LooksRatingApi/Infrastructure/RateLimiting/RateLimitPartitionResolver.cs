using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LooksRatingApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public sealed class RateLimitPartitionResolver
    {
        private const string TelegramPartitionItemKey = "RateLimit:TelegramPartition";

        private static readonly string[] TelegramIdPropertyNames =
        [
            "telegramId",
            "reviewerTelegramId",
            "reporterTelegramId",
        ];

        private readonly ApiKeyAuthOptions _apiKeyOptions;

        public RateLimitPartitionResolver(IOptions<ApiKeyAuthOptions> apiKeyOptions)
        {
            _apiKeyOptions = apiKeyOptions.Value;
        }

        public string ResolveGlobalPartitionKey(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var apiKey)
                && !string.IsNullOrWhiteSpace(apiKey))
            {
                return HashPartition($"apikey:{apiKey}");
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrWhiteSpace(remoteIp)
                ? "anonymous"
                : HashPartition($"ip:{remoteIp}");
        }

        public string ResolveGrpcPartitionKey(string? remoteIp)
        {
            return HashPartition($"grpc:{remoteIp ?? "unknown"}");
        }

        public async Task<string> ResolveTelegramOrFallbackPartitionAsync(
            HttpContext context,
            string globalPartition)
        {
            var telegramPartition = await TryResolveTelegramPartitionKeyAsync(context);
            return telegramPartition ?? $"fallback:{globalPartition}";
        }

        public async Task<string?> TryResolveTelegramPartitionKeyAsync(HttpContext context)
        {
            if (context.Items.TryGetValue(TelegramPartitionItemKey, out var cached)
                && cached is string cachedPartition)
            {
                return cachedPartition;
            }

            string? resolved = null;

            if (context.Request.RouteValues.TryGetValue("telegramId", out var routeValue)
                && long.TryParse(routeValue?.ToString(), out var routeTelegramId)
                && routeTelegramId > 0)
            {
                resolved = $"tg:{routeTelegramId}";
            }
            else if (TryReadTelegramIdFromQuery(context.Request.Query, out var queryTelegramId))
            {
                resolved = $"tg:{queryTelegramId}";
            }
            else if (await TryReadTelegramIdFromJsonBodyAsync(context))
            {
                resolved = context.Items[TelegramPartitionItemKey] as string;
            }

            if (resolved is not null)
            {
                context.Items[TelegramPartitionItemKey] = resolved;
            }

            return resolved;
        }

        private async Task<bool> TryReadTelegramIdFromJsonBodyAsync(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method)
                && !HttpMethods.IsPut(context.Request.Method)
                && !HttpMethods.IsPatch(context.Request.Method))
            {
                return false;
            }

            if (!context.Request.HasJsonContentType())
            {
                return false;
            }

            context.Request.EnableBuffering();

            try
            {
                context.Request.Body.Position = 0;
                using var document = await JsonDocument.ParseAsync(
                    context.Request.Body,
                    cancellationToken: context.RequestAborted);

                if (!TryReadTelegramId(document.RootElement, out var telegramId))
                {
                    return false;
                }

                context.Items[TelegramPartitionItemKey] = $"tg:{telegramId}";
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            finally
            {
                context.Request.Body.Position = 0;
            }
        }

        private static bool TryReadTelegramIdFromQuery(IQueryCollection query, out long telegramId)
        {
            telegramId = 0;

            foreach (var key in query.Keys)
            {
                if (!key.Equals("telegramId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (long.TryParse(query[key], out telegramId) && telegramId > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadTelegramId(JsonElement root, out long telegramId)
        {
            telegramId = 0;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in root.EnumerateObject())
            {
                if (!TelegramIdPropertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.Number
                    && property.Value.TryGetInt64(out telegramId)
                    && telegramId > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string HashPartition(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }
}
