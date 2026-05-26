namespace LooksRatingApi.CQRS.Cities.Query.GetCities
{
    public sealed class GetCitiesResponse
    {
        public IReadOnlyList<string> Cities { get; init; } = Array.Empty<string>();
    }
}
