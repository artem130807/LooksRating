using LooksRatingApi.Infrastructure.DistributedLock;

namespace LooksRatingApi.Tests.Infrastructure.Fakes;

public sealed class TestDistributedLockHandle : IRedisDistributedLockHandle
{
    private readonly Func<TimeSpan, CancellationToken, Task<bool>>? _renew;
    private int _released;

    public TestDistributedLockHandle(
        string key = "test-lock",
        string token = "test-token",
        Func<TimeSpan, CancellationToken, Task<bool>>? renew = null)
    {
        Key = key;
        Token = token;
        _renew = renew;
    }

    public string Key { get; }

    public string Token { get; }

    public Task<bool> RenewAsync(TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (_renew is not null)
        {
            return _renew(ttl, cancellationToken);
        }

        return Task.FromResult(true);
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _released, 1);
        return ValueTask.CompletedTask;
    }
}
