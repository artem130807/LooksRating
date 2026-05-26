using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Cities.Query.GetCities
{
    public sealed record GetCitiesQuery : IRequest<Result<GetCitiesResponse>>;
}
