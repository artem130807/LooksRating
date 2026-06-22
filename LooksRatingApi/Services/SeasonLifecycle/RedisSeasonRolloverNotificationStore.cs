using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Services;
using StackExchange.Redis;

namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class RedisSeasonRolloverNotificationStore : ISeasonRolloverNotificationStore
    {
        private const string ClosedSeasonIdField = "closedSeasonId";
        private const string ClosedSeasonNameField = "closedSeasonName";
        private const string ClosedSeasonNumberField = "closedSeasonNumber";
        private const string NewSeasonIdField = "newSeasonId";
        private const string NewSeasonNameField = "newSeasonName";
        private const string NewSeasonNumberField = "newSeasonNumber";
        private const string CreatedAtUtcField = "createdAtUtc";

        private readonly IDatabase _db;

        public RedisSeasonRolloverNotificationStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<int> TryEnqueueBatchAsync(
            SeasonRolloverEnqueueRequest request,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            if (request.RecipientTelegramIds.Count == 0)
            {
                return 0;
            }

            var eventId = SeasonRolloverEventId.Create(request.ClosedSeasonId, request.NewSeasonId);
            var metaKey = PhotoRedisKeys.SeasonRolloverEventMeta(eventId);
            var pendingKey = PhotoRedisKeys.SeasonRolloverEventPending(eventId);
            var activeKey = PhotoRedisKeys.SeasonRolloverActiveEvents();
            var values = request.RecipientTelegramIds
                .Where(x => x > 0)
                .Distinct()
                .Select(x => (RedisValue)x.ToString())
                .ToArray();

            if (values.Length == 0)
            {
                return 0;
            }

            var metaExists = await _db.KeyExistsAsync(metaKey);
            if (!metaExists)
            {
                var metaEntries = new HashEntry[]
                {
                    new(ClosedSeasonIdField, request.ClosedSeasonId.ToString("D")),
                    new(ClosedSeasonNameField, request.ClosedSeasonName),
                    new(ClosedSeasonNumberField, request.ClosedSeasonNumber),
                    new(NewSeasonIdField, request.NewSeasonId.ToString("D")),
                    new(NewSeasonNameField, request.NewSeasonName),
                    new(NewSeasonNumberField, request.NewSeasonNumber),
                    new(CreatedAtUtcField, DateTime.UtcNow.ToString("O"))
                };

                var transaction = _db.CreateTransaction();
                _ = transaction.HashSetAsync(metaKey, metaEntries);
                _ = transaction.SetAddAsync(pendingKey, values);
                _ = transaction.SetAddAsync(activeKey, eventId);
                _ = transaction.KeyExpireAsync(metaKey, ttl);
                _ = transaction.KeyExpireAsync(pendingKey, ttl);

                if (!await transaction.ExecuteAsync())
                {
                    metaExists = await _db.KeyExistsAsync(metaKey);
                    if (!metaExists)
                    {
                        return 0;
                    }
                }
                else
                {
                    return values.Length;
                }
            }

            var added = 0L;
            foreach (var value in values)
            {
                if (await _db.SetAddAsync(pendingKey, value))
                {
                    added++;
                }
            }

            await _db.KeyExpireAsync(metaKey, ttl);
            await _db.KeyExpireAsync(pendingKey, ttl);
            await _db.SetAddAsync(activeKey, eventId);

            return (int)added;
        }

        public async Task<IReadOnlyList<SeasonRolloverPendingBatch>> GetPendingBatchesAsync(
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
            {
                return Array.Empty<SeasonRolloverPendingBatch>();
            }

            var activeEventIds = await _db.SetMembersAsync(PhotoRedisKeys.SeasonRolloverActiveEvents());
            if (activeEventIds.Length == 0)
            {
                return Array.Empty<SeasonRolloverPendingBatch>();
            }

            foreach (var rawEventId in activeEventIds.OrderBy(x => x.ToString(), StringComparer.Ordinal))
            {
                var eventId = rawEventId.ToString();
                if (string.IsNullOrWhiteSpace(eventId))
                {
                    continue;
                }

                var batch = await BuildPendingBatchAsync(eventId, limit);
                if (batch is not null)
                {
                    return new[] { batch };
                }
            }

            return Array.Empty<SeasonRolloverPendingBatch>();
        }

        public async Task AckDeliveredAsync(
            string eventId,
            IReadOnlyList<long> recipientTelegramIds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(eventId) || recipientTelegramIds.Count == 0)
            {
                return;
            }

            var pendingKey = PhotoRedisKeys.SeasonRolloverEventPending(eventId);
            var values = recipientTelegramIds
                .Where(x => x > 0)
                .Distinct()
                .Select(x => (RedisValue)x.ToString())
                .ToArray();

            if (values.Length == 0)
            {
                return;
            }

            await _db.SetRemoveAsync(pendingKey, values);
            if (await _db.SetLengthAsync(pendingKey) > 0)
            {
                return;
            }

            await _db.KeyDeleteAsync(PhotoRedisKeys.SeasonRolloverEventMeta(eventId));
            await _db.KeyDeleteAsync(pendingKey);
            await _db.SetRemoveAsync(PhotoRedisKeys.SeasonRolloverActiveEvents(), eventId);
        }

        private async Task<SeasonRolloverPendingBatch?> BuildPendingBatchAsync(string eventId, int limit)
        {
            var metaKey = PhotoRedisKeys.SeasonRolloverEventMeta(eventId);
            var pendingKey = PhotoRedisKeys.SeasonRolloverEventPending(eventId);
            var meta = await _db.HashGetAllAsync(metaKey);
            if (meta.Length == 0)
            {
                await CleanupOrphanEventAsync(eventId, pendingKey);
                return null;
            }

            var recipients = new List<long>(limit);
            await foreach (var member in _db.SetScanAsync(pendingKey, pageSize: limit))
            {
                if (long.TryParse(member.ToString(), out var telegramId) && telegramId > 0)
                {
                    recipients.Add(telegramId);
                }

                if (recipients.Count >= limit)
                {
                    break;
                }
            }

            if (recipients.Count == 0)
            {
                await CleanupOrphanEventAsync(eventId, pendingKey);
                return null;
            }

            var metaMap = meta.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString(), StringComparer.Ordinal);
            return new SeasonRolloverPendingBatch
            {
                EventId = eventId,
                ClosedSeasonId = ParseGuid(metaMap, ClosedSeasonIdField),
                ClosedSeasonName = metaMap.GetValueOrDefault(ClosedSeasonNameField) ?? string.Empty,
                ClosedSeasonNumber = ParseInt(metaMap, ClosedSeasonNumberField),
                NewSeasonId = ParseGuid(metaMap, NewSeasonIdField),
                NewSeasonName = metaMap.GetValueOrDefault(NewSeasonNameField) ?? string.Empty,
                NewSeasonNumber = ParseInt(metaMap, NewSeasonNumberField),
                RecipientTelegramIds = recipients
            };
        }

        private async Task CleanupOrphanEventAsync(string eventId, RedisKey pendingKey)
        {
            await _db.KeyDeleteAsync(PhotoRedisKeys.SeasonRolloverEventMeta(eventId));
            await _db.KeyDeleteAsync(pendingKey);
            await _db.SetRemoveAsync(PhotoRedisKeys.SeasonRolloverActiveEvents(), eventId);
        }

        private static Guid ParseGuid(IReadOnlyDictionary<string, string> meta, string field) =>
            Guid.TryParse(meta.GetValueOrDefault(field), out var value) ? value : Guid.Empty;

        private static int ParseInt(IReadOnlyDictionary<string, string> meta, string field) =>
            int.TryParse(meta.GetValueOrDefault(field), out var value) ? value : 0;
    }
}
