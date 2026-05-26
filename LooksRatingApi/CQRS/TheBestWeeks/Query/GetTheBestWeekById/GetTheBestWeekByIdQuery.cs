using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeekById
{
    public sealed record GetTheBestWeekByIdQuery(Guid Id) : IRequest<Result<GetTheBestWeekByIdResponse>>;
}
