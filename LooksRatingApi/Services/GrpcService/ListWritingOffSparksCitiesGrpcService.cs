using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService;

public sealed class ListWritingOffSparksCitiesGrpcService
    : ListWritingOffSparksCitiesService.ListWritingOffSparksCitiesServiceBase
{
    private readonly IListWritingOffSparksCitiesOrchestrator _orchestrator;
    private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

    public ListWritingOffSparksCitiesGrpcService(
        IListWritingOffSparksCitiesOrchestrator orchestrator,
        IOptions<ApiKeyAuthOptions> apiKeyOptions)
    {
        _orchestrator = orchestrator;
        _apiKeyOptions = apiKeyOptions;
    }

    public override async Task<ListWritingOffSparksCitiesResponse> ListWritingOffSparksCities(
        ListWritingOffSparksCitiesRequest request,
        ServerCallContext context)
    {
        GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);
        var result = await _orchestrator.ListCitiesAsync(context.CancellationToken);
        return result.Value;
    }
}
