using Grpc.AspNetCore.Server;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddRedisRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<RateLimitingOptions>()
                .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
                .ValidateOnStart()
                .Services
                .AddSingleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>();

            services.AddHttpContextAccessor();
            services.AddSingleton<RateLimitPartitionResolver>();
            services.AddSingleton<IRateLimitService, RedisRateLimitService>();
            services.AddSingleton<RateLimitGuard>();
            services.AddScoped<RateLimitResourceFilter>();
            services.AddSingleton<RateLimitGrpcInterceptor>();
            services.AddSingleton<IConfigureOptions<GrpcServiceOptions>, ConfigureGrpcRateLimitInterceptor>();

            return services;
        }

        public static IMvcBuilder AddRateLimitingFilters(this IMvcBuilder mvcBuilder)
        {
            return mvcBuilder.AddMvcOptions(options =>
            {
                options.Filters.AddService<RateLimitResourceFilter>();
            });
        }
    }
}
