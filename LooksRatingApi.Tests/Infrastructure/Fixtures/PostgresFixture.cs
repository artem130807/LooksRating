using LooksRatingApi;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LooksRatingApi.Tests.Infrastructure.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
            IsAvailable = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            IsAvailable = false;
            UnavailableReason = ex.Message;
            Console.Error.WriteLine($"[LooksRatingApi.Tests] PostgresFixture failed: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new LooksRatingDbContext(options);
    }
}
