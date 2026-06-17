using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.RecomendationSettings.Command.UpsertRecomendationSettings
{
    public sealed record UpsertRecomendationSettingsCommand(
        long TelegramId,
        int Age,
        GenderEnum Gender,
        string City) : IRequest<Result<Unit>>;
}
