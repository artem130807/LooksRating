using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// Add services to the container.
builder.Services.AddDb(configuration);
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ILoadingCityService, LoadingCityService>();
builder.Services.AddSingleton<ILoadingBadWordService, LoadingBadWordService>();
builder.Services.AddScoped<ICityService, CityService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IUserTicketRepository, UserTicketRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPhotoUserRepository, PhotoUserRepository>();
builder.Services.AddScoped<ITheBestWeekRepository, TheBestWeekRepository>();
builder.Services.AddMemoryCache();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var loader = scope.ServiceProvider.GetRequiredService<ILoadingCityService>();
    loader.CreateCityNames(env);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
