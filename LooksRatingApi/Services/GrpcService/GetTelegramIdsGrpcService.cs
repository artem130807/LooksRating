using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.GrpcService
{
    public class GetTelegramIdsGrpcService : GetTelegramIdsService.GetTelegramIdsServiceBase
    {
        private readonly IVipTopRewardOrchestrator _vipTopRewardOrchestrator;

        public GetTelegramIdsGrpcService(IVipTopRewardOrchestrator vipTopRewardOrchestrator)
        {
            _vipTopRewardOrchestrator = vipTopRewardOrchestrator;
        }

        public override async Task<GetTelegramIdsResponse> GetTelegramIds(
            GetTelegramIdsRequest request,
            ServerCallContext context)
        {
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

            return response;
        }
    }
}
