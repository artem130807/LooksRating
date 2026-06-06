using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.GrpcService
{
    public class GetTelegramIdsGrpcService : GetTelegramIdsService.GetTelegramIdsServiceBase
    {
        private readonly IVipTopRewardOrchestrator _vipTopRewardOrchestrator;
        private readonly ILogger<GetTelegramIdsGrpcService> _logger;

        public GetTelegramIdsGrpcService(
            IVipTopRewardOrchestrator vipTopRewardOrchestrator,
            ILogger<GetTelegramIdsGrpcService> logger)
        {
            _vipTopRewardOrchestrator = vipTopRewardOrchestrator;
            _logger = logger;
        }

        public override async Task<GetTelegramIdsResponse> GetTelegramIds(
            GetTelegramIdsRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetTelegramIds: запрос от {Peer}", context.Peer);

            var candidates = await _vipTopRewardOrchestrator.ProcessAndGetProfilesAsync(context.CancellationToken);
            var response = new GetTelegramIdsResponse();

            foreach (var candidate in candidates)
            {
                response.Profiles.Add(new VipTopProfileEntry
                {
                    TelegramId = candidate.TelegramId,
                    City = candidate.City,
                    Rating = (double)candidate.Rating,
                    RatingCount = candidate.RatingCount,
                    Age = candidate.Age,
                    Gender = (int)candidate.Gender,
                    CreatedAtUnix = new DateTimeOffset(candidate.CreatedAt.ToUniversalTime()).ToUnixTimeSeconds(),
                });
            }

            _logger.LogInformation("gRPC GetTelegramIds: возвращено {Count} профилей", candidates.Count);
            return response;
        }
    }
}
