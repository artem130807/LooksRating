using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService
{
    public sealed class RollBackDebitedSparksGrpcService : RollBackDebitedSparksService.RollBackDebitedSparksServiceBase
    {
        private readonly IRollBackDebitedSparksOrchestrator _rollBackDebitedSparksOrchestrator;
        private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

        public RollBackDebitedSparksGrpcService(
            IRollBackDebitedSparksOrchestrator rollBackDebitedSparksOrchestrator,
            IOptions<ApiKeyAuthOptions> apiKeyOptions)
        {
            _rollBackDebitedSparksOrchestrator = rollBackDebitedSparksOrchestrator;
            _apiKeyOptions = apiKeyOptions;
        }

        public override async Task<RollBackDebitedSparksResponse> RollBackDebitedSparks(
            RollBackDebitedSparksRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);
            var result = await _rollBackDebitedSparksOrchestrator.RollBackDebitedSparks(
                request.TelegramId,
                request.StarsCount,
                request.Reason,
                string.IsNullOrWhiteSpace(request.Key) ? null : request.Key,
                context.CancellationToken);

            if (result.IsFailure)
            {
                return new RollBackDebitedSparksResponse
                {
                    Success = false,
                    Message = result.Error
                };
            }

            return result.Value;
        }
    }
}
