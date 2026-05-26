using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public class ArchivingLockService
    {
        private readonly StackExchange.Redis.IDatabase _redis;
        private const string ARCHIVE_LOCK_KEY = "archive:in_progress";

        public ArchivingLockService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task<bool> IsArchivingInProgressAsync()
        {
            return await _redis.KeyExistsAsync(ARCHIVE_LOCK_KEY);
        }

        public async Task StartArchivingAsync(TimeSpan ttl)
        {
            await _redis.StringSetAsync(ARCHIVE_LOCK_KEY, "locked", ttl);
        }

        public async Task EndArchivingAsync()
        {
            await _redis.KeyDeleteAsync(ARCHIVE_LOCK_KEY);
        }
    }
}