using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Health;
using LooksRatingApi.Infrastructure.Quartz;
using LooksRatingApi.Infrastructure.RateLimiting;
using LooksRatingApi.Models;
using LooksRatingApi.Infrastructure.SparksWallet;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Services.GrpcService;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

namespace LooksRatingApi.Infrastructure.Startup
{
    public static class ApplicationStartupExtensions
    {
        public static WebApplicationBuilder ConfigureHost(this WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
                options.ListenAnyIP(8081, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            });

            return builder;
        }

        public static IServiceCollection AddApplicationInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddApiKeyAuthentication(configuration);
            services.AddRedisRateLimiting(configuration);
            services.AddInfrastructureHealthChecks(configuration);
            services.AddScoped<ISeasonDataSeeder, SeasonDataSeeder>();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "LooksRating API",
                    Version = "v1"
                });

                options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "API key for bot and trusted clients",
                    Name = configuration["Security:HeaderName"] ?? "X-Api-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        public static async Task InitializeApplicationAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationStartup");

            var dbContext = services.GetRequiredService<LooksRatingDbContext>();
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();

            logger.LogInformation("Ensuring sparks wallet schema...");
            await SparksWalletSchemaBootstrap.EnsureAsync(dbContext);

            logger.LogInformation("Ensuring Quartz schema...");
            var quartzSchemaBootstrap = services.GetRequiredService<QuartzSchemaBootstrap>();
            await quartzSchemaBootstrap.EnsureCreatedAsync();

            await VipProductBootstrap.EnsureConfiguredAsync(dbContext);

            var env = services.GetRequiredService<IWebHostEnvironment>();
            var cityNames = services.GetRequiredService<ILoadingCityService>().CreateCityNames(env);
            if (cityNames.Count == 0)
            {
                throw new InvalidOperationException("Data/cities.json не содержит городов — фоновые задачи не могут работать.");
            }

            services.GetRequiredService<ILoadingBadWordService>().CreateBadWord(env);

            logger.LogInformation("Seeding seasons...");
            await services.GetRequiredService<ISeasonDataSeeder>().SeedAsync();
            logger.LogInformation("Application initialization completed");
        }

        public static WebApplication ConfigureApplicationPipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "LooksRating API v1");
                });
            }

            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapInfrastructureHealthChecks();
            app.MapControllers();
            app.MapGrpcService<GetTelegramIdsGrpcService>().AllowAnonymous();
            app.MapGrpcService<GetUsersForMessageGrpcService>().AllowAnonymous();
            app.MapGrpcService<CurrentSparksForUserGrpcService>().AllowAnonymous();
            app.MapGrpcService<DebitedSparksGrpcService>().AllowAnonymous();
            app.MapGrpcService<RollBackDebitedSparksGrpcService>().AllowAnonymous();
            app.MapGrpcService<AdminTicketGrpcService>().AllowAnonymous();
            app.MapGrpcService<RemoveTicketsPhotoprofileGrpcService>().AllowAnonymous();
            app.MapGrpcService<RejectTicketPhotoProfileGrpcService>().AllowAnonymous();

            return app;
        }
    }
}
