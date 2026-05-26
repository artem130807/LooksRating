using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasonById
{
    public sealed record GetListSeasonByIdQuery(Guid Id) : IRequest<Result<ListSeasonResponse>>;
}
