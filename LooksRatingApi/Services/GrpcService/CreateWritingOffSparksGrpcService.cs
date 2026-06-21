using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService
{
    public class CreateWritingOffSparksGrpcService : CreateWritingOffSparksService.CreateWritingOffSparksServiceBase
    {
        private readonly ICreateWritingOffSparksOrchestrator _createWritingOffSparksOrchestrator;
        private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

        public CreateWritingOffSparksGrpcService(
            ICreateWritingOffSparksOrchestrator createWritingOffSparksOrchestrator,
            IOptions<ApiKeyAuthOptions> apiKeyOptions)
        {
            _createWritingOffSparksOrchestrator = createWritingOffSparksOrchestrator;
            _apiKeyOptions = apiKeyOptions;
        }

        public override async Task<CreateWritingOffSparksResponse> CreateWritingOffSparks(
            CreateWritingOffSparksRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);
            var writingOffSparks = await _createWritingOffSparksOrchestrator.ConfirmedWriting(
                request.TelegramId,
                request.SparksCount,
                request.Key,
                request.StarsCount,
                context.CancellationToken);
            return writingOffSparks.Value;
        }
    }
}
