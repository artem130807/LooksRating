using LooksRatingApi;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.CQRS.RecomendationSettings.Command.UpsertRecomendationSettings;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket;
using LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos;
using LooksRatingApi.CQRS.Users.Command.UpdateUserAge;
using LooksRatingApi.CQRS.Users.Command.UpdateUserCity;
using LooksRatingApi.Cqrs.Users.Command.RegisterUser;
using LooksRatingApi.CQRS.Users.Command.UpdateGenderUser;
using LooksRatingApi.Infrastructure.Startup;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using MediatR;
using StackExchange.Redis;
using System.Text.Json.Serialization;
using LooksRatingApi.Services.BackGroundServices.Handlers;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.ConfigureHost();

builder.Services.AddDb(configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApplicationInfrastructure(configuration);
builder.Services.AddMediatR(typeof(Program));

builder.Services.AddSingleton<ILoadingCityService, LoadingCityService>();
builder.Services.AddSingleton<ILoadingBadWordService, LoadingBadWordService>();
builder.Services.AddScoped<ICityService, CityService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRecomendationSettingsRepository, RecomendationSettingsRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IUserTicketRepository, UserTicketRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPhotoUserRepository, PhotoUserRepository>();
builder.Services.AddScoped<ITheBestWeekRepository, TheBestWeekRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();
builder.Services.AddScoped<IListSeasonsRepository, ListSeasonsRepository>();
builder.Services.AddScoped<IUserRegisterValidator, UserRegisterValidator>();
builder.Services.AddScoped<IUpdateGenderUserValidator, UpdateGenderUserValidator>();
builder.Services.AddScoped<ISetUserPhotoValidator, SetUserPhotoValidator>();
builder.Services.AddScoped<IRecreateUserPhotoValidator, RecreateUserPhotoValidator>();
builder.Services.AddScoped<IPhotoUserLifecycleService, PhotoUserLifecycleService>();
builder.Services.AddScoped<IUpdateUserCityValidator, UpdateUserCityValidator>();
builder.Services.AddScoped<IUpdateUserAgeValidator, UpdateUserAgeValidator>();
builder.Services.AddScoped<IUpsertRecomendationSettingsValidator, UpsertRecomendationSettingsValidator>();
builder.Services.AddScoped<ICreateReviewValidator, CreateReviewValidator>();
builder.Services.AddScoped<ICreateUserTicketValidator, CreateUserTicketValidator>();
builder.Services.AddScoped<IGetTopUserPhotosValidator, GetTopUserPhotosValidator>();
builder.Services.AddScoped<IGetTheBestWeekPhotosNowValidator, GetTheBestWeekPhotosNowValidator>();
builder.Services.AddScoped<INormalizeCityNameService, NormalizeCityNameService>();
builder.Services.AddScoped<IRankService, RankService>();
builder.Services.AddScoped<IUpdateRatingPhotoService, UpdateRatingPhotoService>();
builder.Services.AddScoped<IPhotoRatingCacheService, PhotoRatingCacheService>();
builder.Services.AddScoped<IPhotoTopReadService, PhotoTopReadService>();
builder.Services.AddScoped<IAddPhotoUsersCacheHandler, AddPhotoUsersCacheHandler>();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisUrl = configuration.GetConnectionString("Redis") ?? "127.0.0.1:6379,abortConnect=false";
    var options = ConfigurationOptions.Parse(redisUrl);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
builder.Services.AddMemoryCache();
builder.Services.AddBackGroundService(configuration);
builder.Services.AddQuartz(configuration);

var app = builder.Build();

await app.InitializeApplicationAsync();
app.ConfigureApplicationPipeline();

app.Run();
