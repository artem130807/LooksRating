using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Query.GetCurrentSeason
{
    public sealed record GetCurrentSeasonQuery(Guid? ListSeasonsId = null)
        : IRequest<Result<SeasonResponse>>;
}
