using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Google.Protobuf.WellKnownTypes;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Models;
using LooksRatingGrpc;
using StackExchange.Redis;

namespace LooksRatingApi.Services.Orchestrators
{
    public class GetUnActiveUsersOrchestrator:IGetUnActiveUsersOrchestrator
    {
        private readonly IConnectionMultiplexer _connection;
        private  readonly IDatabase _redis;
        private readonly IUserRepository _userRepository;
        public GetUnActiveUsersOrchestrator(IConnectionMultiplexer connection, IUserRepository userRepository)
        {
            _connection = connection;
            _redis = connection.GetDatabase();
            _userRepository = userRepository;
        }
        public async Task<Result<GetUnActiveUsersResponse>> GetUsers(CancellationToken cancellationToken = default)
        {
            int batchSize = 50;
            int page = 1;
            var redisUserIds = new List<long>();    
            while (true)
            {
                var users = await _userRepository.GetUsersToPagedAsync(new Filters.PageParams{Page = page, PageSize = 50});
                if (users.Data.Count == 0)
                {
                    break;
                }
                var ids = users.Data.Select(u => u.Id).ToList();
                foreach (var id in ids)
                {
                    var keyRedis = $"last_active_{id}";
                    var idRedis = await _redis.StringGetAsync(keyRedis);
                    if (idRedis != RedisValue.EmptyString)
                    {
                        redisUserIds.Add(long.Parse(idRedis.ToString()));
                    }
                }
                if (users.Data.Count < batchSize)
                {
                    break;
                }
                page++;
            }

            var response = new GetUnActiveUsersResponse();
            response.TelegramIds.AddRange(redisUserIds);
            return Result.Success(response);
        }
    }
}