using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LooksRatingApi.Infrastructure.Health
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddInfrastructureHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var postgresConnection = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured");

            var redisConnection = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Connection string 'Redis' is not configured");

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
                .AddNpgSql(postgresConnection, name: "postgresql", tags: ["ready"])
                .AddRedis(redisConnection, name: "redis", tags: ["ready"])
                .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"]);

            return services;
        }

        public static WebApplication MapInfrastructureHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            }).AllowAnonymous();

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteReadyResponse
            }).AllowAnonymous();

            return app;
        }

        private static Task WriteReadyResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            var status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy";
            var entries = report.Entries.Select(e =>
                $"\"{e.Key}\":{{\"status\":\"{e.Value.Status}\"}}");

            var json = $"{{\"status\":\"{status}\",\"checks\":{{{string.Join(',', entries)}}}}}";
            return context.Response.WriteAsync(json);
        }
    }
}
