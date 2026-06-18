using Grpc.Core;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.GrpcService
{
    public sealed class GetUsersForMessageGrpcService : GetUsersForMessageService.GetUsersForMessageServiceBase
    {
        private const int DefaultPageSize = 100;
        private const int MaxPageSize = 500;

        private readonly IUserRepository _userRepository;

        public GetUsersForMessageGrpcService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public override async Task<GetUsersForMessageResponse> GetUsersForMessage(
            GetUsersForMessageRequest request,
            ServerCallContext context)
        {
            var page = request.Page > 0 ? request.Page : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : DefaultPageSize;
            pageSize = Math.Min(pageSize, MaxPageSize);

            var paged = await _userRepository.GetTelegramIdsPagedAsync(
                page,
                pageSize,
                request.OnlyUnsubscribedChannel,
                context.CancellationToken);

            var loadedCount = page * pageSize;
            var hasNextPage = loadedCount < paged.Count;

            var response = new GetUsersForMessageResponse
            {
                TotalCount = paged.Count,
                Page = page,
                PageSize = pageSize,
                HasNextPage = hasNextPage,
            };
            response.TelegramIds.AddRange(paged.Data);

            return response;
        }
    }
}
