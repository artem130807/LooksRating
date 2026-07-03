using Grpc.Core;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.GrpcService;

public sealed class GetUnActiveUsersGrpcService : GetUnActiveUsersService.GetUnActiveUsersServiceBase
{
    private readonly IGetUnActiveUsersOrchestrator _getUnActiveUsersOrchestrator;
    public GetUnActiveUsersGrpcService(IGetUnActiveUsersOrchestrator getUnActiveUsersOrchestrator)
    {
        _getUnActiveUsersOrchestrator = getUnActiveUsersOrchestrator;
    }
    public override async Task<GetUnActiveUsersResponse> GetUnActiveUsers(
        GetUnActiveUsersRequest request,
        ServerCallContext context)
    {
        var result = await _getUnActiveUsersOrchestrator.GetUsers(context.CancellationToken);
        if (result.IsFailure)
        {
            throw new RpcException(new Status(StatusCode.Internal, result.Error));
        }
        return result.Value;
    }
}
