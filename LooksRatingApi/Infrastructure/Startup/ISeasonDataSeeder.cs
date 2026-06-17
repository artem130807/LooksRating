namespace LooksRatingApi.Infrastructure.Startup
{
    public interface ISeasonDataSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}
