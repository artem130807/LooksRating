namespace LooksRatingApi.Infrastructure.TestQuartz
{
    public interface ITestQuartzDataSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}
