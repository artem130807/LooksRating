using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService;

public sealed class GetWritingOffSparksGrpcService : GetWritingOffSparksService.GetWritingOffSparksServiceBase
{
    private readonly IGetWritingOffSparksOrchestrator _orchestrator;
    private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

    public GetWritingOffSparksGrpcService(
        IGetWritingOffSparksOrchestrator orchestrator,
        IOptions<ApiKeyAuthOptions> apiKeyOptions)
    {
        _orchestrator = orchestrator;
        _apiKeyOptions = apiKeyOptions;
    }

    public override async Task<GetWritingOffSparksResponse> GetWritingOffSparks(
        GetWritingOffSparksRequest request,
        ServerCallContext context)
    {
        GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);
        if (!Guid.TryParse(request.WritingOffSparksId, out var writingOffSparksId))
        {
            return new GetWritingOffSparksResponse
            {
                Success = false,
                Message = "Некорректный идентификатор списания искр",
            };
        }

        var result = await _orchestrator.GetByIdAsync(writingOffSparksId, context.CancellationToken);
        return result.Value;
    }
}
