using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Health;
using LooksRatingApi.Models;
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
        private const int VipProductCode = 1001;
        private const int VipTestStarsPrice = 1;
        private const int VipDays = 30;

        public static WebApplicationBuilder ConfigureHost(this WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureEndpointDefaults(listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
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

            var vipProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductCode == VipProductCode);
            if (vipProduct is null)
            {
                var productResult = Product.Create("VIP-статус", VipProductCode, VipTestStarsPrice, "XTR", VipDays);
                if (productResult.IsSuccess)
                {
                    dbContext.Products.Add(productResult.Value);
                    await dbContext.SaveChangesAsync();
                }
            }
            else if (vipProduct.CountStars != VipTestStarsPrice
                || !vipProduct.IsActive
                || !string.Equals(vipProduct.Currency, "XTR", StringComparison.OrdinalIgnoreCase)
                || vipProduct.VipDays != VipDays)
            {
                dbContext.Entry(vipProduct).Property(nameof(Product.CountStars)).CurrentValue = VipTestStarsPrice;
                dbContext.Entry(vipProduct).Property(nameof(Product.IsActive)).CurrentValue = true;
                dbContext.Entry(vipProduct).Property(nameof(Product.Currency)).CurrentValue = "XTR";
                dbContext.Entry(vipProduct).Property(nameof(Product.VipDays)).CurrentValue = VipDays;
                dbContext.Entry(vipProduct).Property(nameof(Product.UpdatedAt)).CurrentValue = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

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
            app.MapGrpcService<GetTelegramIdsGrpcService>();

            return app;
        }
    }
}
