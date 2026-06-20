using LooksRatingApi.Contracts.PhotoUserContracts;
using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public sealed class FeedCycleRedisStore : IFeedCycleStore
    {
        private readonly IDatabase _db;

        public FeedCycleRedisStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<HashSet<Guid>> GetRatedProfileIdsAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            var members = await _db.SetMembersAsync(PhotoRedisKeys.UserRatedSet(reviewerUserId, seasonId));
            var ratedProfileIds = new HashSet<Guid>(members.Length);
            foreach (var member in members)
            {
                if (Guid.TryParse(member.ToString(), out var profileId))
                {
                    ratedProfileIds.Add(profileId);
                }
            }

            return ratedProfileIds;
        }

        public async Task<int> GetFeedRatingCounterAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            var value = await _db.StringGetAsync(PhotoRedisKeys.FeedRatingCounter(reviewerUserId, seasonId));
            if (!value.HasValue || !long.TryParse(value.ToString(), out var count) || count < 0)
            {
                return 0;
            }

            return count > int.MaxValue ? int.MaxValue : (int)count;
        }

        public async Task EnsureCycleAnchorAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            var key = PhotoRedisKeys.CycleAnchor(reviewerUserId, seasonId);
            if (!await _db.KeyExistsAsync(key))
            {
                await SetCycleAnchorAsync(key, DateTime.UtcNow);
            }
        }

        public async Task<DateTime> GetCycleAnchorAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            var value = await _db.StringGetAsync(PhotoRedisKeys.CycleAnchor(reviewerUserId, seasonId));
            if (!value.HasValue || !long.TryParse(value.ToString(), out var ticks))
            {
                return DateTime.UtcNow;
            }

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        public async Task ResetCycleAsync(
            Guid reviewerUserId,
            Guid seasonId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            await _db.KeyDeleteAsync(PhotoRedisKeys.UserRatedSet(reviewerUserId, seasonId));
            await SetCycleAnchorAsync(PhotoRedisKeys.CycleAnchor(reviewerUserId, seasonId), utcNow);
            await _db.StringSetAsync(PhotoRedisKeys.SkipFeedRepair(reviewerUserId, seasonId), "1");
        }

        public async Task AddRatedProfileIdsAsync(
            Guid reviewerUserId,
            Guid seasonId,
            IReadOnlyCollection<Guid> profileIds,
            CancellationToken cancellationToken = default)
        {
            if (profileIds.Count == 0)
            {
                return;
            }

            var ratedKey = PhotoRedisKeys.UserRatedSet(reviewerUserId, seasonId);
            var values = profileIds.Select(id => (RedisValue)id.ToString()).ToArray();
            await _db.SetAddAsync(ratedKey, values);
        }

        public async Task<bool> TryMarkProfileAsServedAsync(
            Guid reviewerUserId,
            Guid seasonId,
            Guid profileId,
            CancellationToken cancellationToken = default)
        {
            var added = await _db.SetAddAsync(
                PhotoRedisKeys.UserRatedSet(reviewerUserId, seasonId),
                profileId.ToString());

            if (added)
            {
                await _db.StringIncrementAsync(
                    PhotoRedisKeys.FeedRatingCounter(reviewerUserId, seasonId));
            }

            await _db.KeyDeleteAsync(PhotoRedisKeys.SkipFeedRepair(reviewerUserId, seasonId));
            return added;
        }

        public Task<bool> ShouldSkipRepairFromReviewsAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default) =>
            _db.KeyExistsAsync(PhotoRedisKeys.SkipFeedRepair(reviewerUserId, seasonId));

        private Task SetCycleAnchorAsync(RedisKey cycleAnchorKey, DateTime utcNow) =>
            _db.StringSetAsync(cycleAnchorKey, utcNow.Ticks.ToString());
    }
}
