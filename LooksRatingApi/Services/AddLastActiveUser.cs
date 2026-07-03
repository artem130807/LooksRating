using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public class AddLastActiveUser : IAddLastActiveUser
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _redis;
        public AddLastActiveUser(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _redis = _connectionMultiplexer.GetDatabase();
        }
        public async Task Add(Guid userId, long telegramId,CancellationToken cancellationToken = default)
        {
            var key = $"last_active_{userId}";
            await _redis.StringSetAsync(key, telegramId, TimeSpan.FromHours(72));
        }
    }
} 