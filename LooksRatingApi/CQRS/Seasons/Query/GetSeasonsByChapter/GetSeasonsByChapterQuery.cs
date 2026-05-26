using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Query.GetSeasonsByChapter
{
    public sealed record GetSeasonsByChapterQuery(Guid ListSeasonsId, bool IncludeClosed = true)
        : IRequest<Result<List<SeasonSummaryResponse>>>;
}
