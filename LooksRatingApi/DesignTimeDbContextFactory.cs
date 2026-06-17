using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LooksRatingApi
{
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LooksRatingDbContext>
    {
        public LooksRatingDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");

            var optionsBuilder = new DbContextOptionsBuilder<LooksRatingDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new LooksRatingDbContext(optionsBuilder.Options);
        }
    }
}
