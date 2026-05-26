using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Health;
using LooksRatingApi.Services.CityServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

namespace LooksRatingApi.Infrastructure.Startup
{
    public static class ApplicationStartupExtensions
    {
        public static WebApplicationBuilder ConfigureHost(this WebApplicationBuilder builder)
        {
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

            var dbContext = services.GetRequiredService<LooksRatingDbContext>();
            await dbContext.Database.MigrateAsync();

            var env = services.GetRequiredService<IWebHostEnvironment>();
            services.GetRequiredService<ILoadingCityService>().CreateCityNames(env);
            services.GetRequiredService<ILoadingBadWordService>().CreateBadWord(env);

            await services.GetRequiredService<ISeasonDataSeeder>().SeedAsync();
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

            return app;
        }
    }
}
