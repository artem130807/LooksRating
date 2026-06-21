using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService;

public sealed class GetWritingsOffSparksGrpcService : GetWritingsOffSparksService.GetWritingsOffSparksServiceBase
{
    private readonly IGetWritingsOffSparksOrchestrator _orchestrator;
    private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

    public GetWritingsOffSparksGrpcService(
        IGetWritingsOffSparksOrchestrator orchestrator,
        IOptions<ApiKeyAuthOptions> apiKeyOptions)
    {
        _orchestrator = orchestrator;
        _apiKeyOptions = apiKeyOptions;
    }

    public override async Task<GetWritingsOffSparksResponse> GetWritingsOffSparks(
        GetWritingsOffSparksRequest request,
        ServerCallContext context)
    {
        GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);
        if (string.IsNullOrWhiteSpace(request.City))
        {
            return new GetWritingsOffSparksResponse
            {
                Success = false,
                Message = "Город не указан",
            };
        }

        var result = await _orchestrator.GetByCityAsync(
            request.City,
            request.Page,
            request.PageSize,
            context.CancellationToken);

        return result.Value;
    }
}
