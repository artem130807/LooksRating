using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasons
{
    public sealed record GetListSeasonsQuery(bool IncludeSeasons = false)
        : IRequest<Result<List<ListSeasonResponse>>>;
}
