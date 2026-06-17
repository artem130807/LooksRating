namespace LooksRatingApi.CQRS.RecomendationSettings.Query.GetRecomendationSettings
{
    public sealed record GetRecomendationSettingsQuery(long TelegramId)
        : MediatR.IRequest<CSharpFunctionalExtensions.Result<GetRecomendationSettingsResponse>>;
}
