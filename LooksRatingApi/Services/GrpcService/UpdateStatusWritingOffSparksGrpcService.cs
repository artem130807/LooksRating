using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingApi.Services.Grpc.Mapping;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService;

public sealed class UpdateStatusWritingOffSparksGrpcService
    : UpdateStatusWritingOffSparksService.UpdateStatusWritingOffSparksServiceBase
{
    private readonly IUpdateStatusWritingOffSparksOrchestrator _orchestrator;
    private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

    public UpdateStatusWritingOffSparksGrpcService(
        IUpdateStatusWritingOffSparksOrchestrator orchestrator,
        IOptions<ApiKeyAuthOptions> apiKeyOptions)
    {
        _orchestrator = orchestrator;
        _apiKeyOptions = apiKeyOptions;
    }

    public override async Task<UpdateStatusWritingOffSparksResponse> UpdateStatusWritingOffSparks(
        UpdateStatusWritingOffSparksRequest request,
        ServerCallContext context)
    {
        GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);
        if (!Guid.TryParse(request.WritingOffSparksId, out var writingOffSparksId))
        {
            return new UpdateStatusWritingOffSparksResponse
            {
                Success = false,
                Message = "Некорректный идентификатор списания искр",
            };
        }

        if (!OutputStatusEnumMapper.TryToDomain(request.Status, out var status))
        {
            return new UpdateStatusWritingOffSparksResponse
            {
                Success = false,
                Message = "Некорректный статус списания искр",
            };
        }

        var result = await _orchestrator.UpdateStatusAsync(
            writingOffSparksId,
            status,
            context.CancellationToken);

        return result.Value;
    }
}
