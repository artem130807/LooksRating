using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetLatestListSeason
{
    public sealed record GetLatestListSeasonQuery(bool IncludeSeasons = true)
        : IRequest<Result<ListSeasonResponse>>;
}
