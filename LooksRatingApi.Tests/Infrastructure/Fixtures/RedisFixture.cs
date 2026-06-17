using StackExchange.Redis;
using Testcontainers.Redis;

namespace LooksRatingApi.Tests.Infrastructure.Fixtures;

public sealed class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _container;
    private IConnectionMultiplexer? _connection;

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public IConnectionMultiplexer Connection =>
        _connection ?? throw new InvalidOperationException(
            UnavailableReason is null
                ? "Redis fixture is not initialized."
                : $"Redis fixture is unavailable: {UnavailableReason}");

    public async Task InitializeAsync()
    {
        try
        {
            _container = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            _connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
            IsAvailable = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            IsAvailable = false;
            UnavailableReason = ex.Message;
            Console.Error.WriteLine($"[LooksRatingApi.Tests] RedisFixture failed: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Obsolete("Use Connection property to reuse a single multiplexer per fixture.")]
    public IConnectionMultiplexer CreateConnection() => Connection;
}
