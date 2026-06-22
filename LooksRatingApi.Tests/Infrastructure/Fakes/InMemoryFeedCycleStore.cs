using LooksRatingApi.Contracts.PhotoUserContracts;

namespace LooksRatingApi.Tests.Infrastructure.Fakes;

/// <summary>
/// In-process IFeedCycleStore for unit tests without Docker/Redis.
/// Mirrors FeedCycleRedisStore semantics.
/// </summary>
public sealed class InMemoryFeedCycleStore : IFeedCycleStore
{
    private readonly object _sync = new();
    private readonly Dictionary<(Guid UserId, Guid SeasonId), HashSet<Guid>> _ratedSets = new();
    private readonly Dictionary<(Guid UserId, Guid SeasonId), int> _counters = new();
    private readonly Dictionary<(Guid UserId, Guid SeasonId), DateTime> _anchors = new();
    private readonly HashSet<(Guid UserId, Guid SeasonId)> _skipRepair = new();

    public Task<HashSet<Guid>> GetRatedProfileIdsAsync(
        Guid reviewerUserId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(
                _ratedSets.TryGetValue((reviewerUserId, seasonId), out var rated)
                    ? new HashSet<Guid>(rated)
                    : new HashSet<Guid>());
        }
    }

    public Task<int> GetFeedRatingCounterAsync(
        Guid reviewerUserId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(
                _counters.TryGetValue((reviewerUserId, seasonId), out var count) ? count : 0);
        }
    }

    public Task EnsureCycleAnchorAsync(
        Guid reviewerUserId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = (reviewerUserId, seasonId);
            if (!_anchors.ContainsKey(key))
            {
                _anchors[key] = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    public Task<DateTime> GetCycleAnchorAsync(
        Guid reviewerUserId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(
                _anchors.TryGetValue((reviewerUserId, seasonId), out var anchor)
                    ? anchor
                    : DateTime.UtcNow);
        }
    }

    public Task ResetCycleAsync(
        Guid reviewerUserId,
        Guid seasonId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _ratedSets.Remove((reviewerUserId, seasonId));
            _anchors[(reviewerUserId, seasonId)] = utcNow;
            _skipRepair.Add((reviewerUserId, seasonId));
        }

        return Task.CompletedTask;
    }

    public Task AddRatedProfileIdsAsync(
        Guid reviewerUserId,
        Guid seasonId,
        IReadOnlyCollection<Guid> profileIds,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = (reviewerUserId, seasonId);
            if (!_ratedSets.TryGetValue(key, out var rated))
            {
                rated = new HashSet<Guid>();
                _ratedSets[key] = rated;
            }

            foreach (var profileId in profileIds)
            {
                rated.Add(profileId);
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryMarkProfileAsServedAsync(
        Guid reviewerUserId,
        Guid seasonId,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = (reviewerUserId, seasonId);
            if (!_ratedSets.TryGetValue(key, out var rated))
            {
                rated = new HashSet<Guid>();
                _ratedSets[key] = rated;
            }

            if (!rated.Add(profileId))
            {
                return Task.FromResult(false);
            }

            _counters.TryGetValue(key, out var count);
            _counters[key] = count + 1;
            _skipRepair.Remove(key);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ShouldSkipRepairFromReviewsAsync(
        Guid reviewerUserId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_skipRepair.Contains((reviewerUserId, seasonId)));
        }
    }

    public HashSet<Guid> SnapshotRated(Guid reviewerUserId, Guid seasonId)
    {
        lock (_sync)
        {
            return _ratedSets.TryGetValue((reviewerUserId, seasonId), out var rated)
                ? new HashSet<Guid>(rated)
                : new HashSet<Guid>();
        }
    }
}
