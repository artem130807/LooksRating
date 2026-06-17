using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Query.GetSeasonById
{
    public sealed record GetSeasonByIdQuery(Guid Id, bool IncludeChapter = false)
        : IRequest<Result<SeasonResponse>>;
}
