using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService
{
    public class DebitedSparksGrpcService : DebitedSparksService.DebitedSparksServiceBase
    {
        private readonly IDebitedSparksOrchestrator _debitedSparksOrchestrator;
        private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

        public DebitedSparksGrpcService(
            IDebitedSparksOrchestrator debitedSparksOrchestrator,
            IOptions<ApiKeyAuthOptions> apiKeyOptions)
        {
            _debitedSparksOrchestrator = debitedSparksOrchestrator;
            _apiKeyOptions = apiKeyOptions;
        }

        public override async Task<DebitedSparksResponse> DebitedSparks(
            DebitedSparksRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);
            var result = await _debitedSparksOrchestrator.DebitedSparks(
                request.TelegramId,
                request.SparksCount,
                string.IsNullOrWhiteSpace(request.Key) ? null : request.Key,
                context.CancellationToken);

            if (result.IsFailure)
            {
                return new DebitedSparksResponse
                {
                    Success = false,
                    Message = result.Error
                };
            }

            return result.Value;
        }
    }
}
