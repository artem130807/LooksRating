using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using MediatR;

namespace LooksRatingApi.CQRS.Cities.Query.GetCities
{
    public sealed class GetCitiesHandler : IRequestHandler<GetCitiesQuery, Result<GetCitiesResponse>>
    {
        private readonly ICityService _cityService;

        public GetCitiesHandler(ICityService cityService)
        {
            _cityService = cityService;
        }

        public Task<Result<GetCitiesResponse>> Handle(GetCitiesQuery request, CancellationToken cancellationToken)
        {
            var cities = _cityService.GetAllCities();
            return Task.FromResult(Result.Success(new GetCitiesResponse { Cities = cities }));
        }
    }
}
