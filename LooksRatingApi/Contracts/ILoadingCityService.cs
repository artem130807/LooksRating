namespace LooksRatingApi.Contracts
{
    public interface ILoadingCityService
    {
        HashSet<string> CreateCityNames(IWebHostEnvironment env);

        HashSet<string> GetCityNames();
    }
}
