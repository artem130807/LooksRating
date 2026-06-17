using Grpc.Core;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService
{
    public sealed class RemoveTicketsPhotoprofileGrpcService : RemoveTicketsPhotoprofileService.RemoveTicketsPhotoprofileServiceBase
    {
        private readonly IRemoveTicketsPhotoprofileOrchestrator _removeTicketsPhotoprofileOrchestrator;
        private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

        public RemoveTicketsPhotoprofileGrpcService(
            IRemoveTicketsPhotoprofileOrchestrator removeTicketsPhotoprofileOrchestrator,
            IOptions<ApiKeyAuthOptions> apiKeyOptions)
        {
            _removeTicketsPhotoprofileOrchestrator = removeTicketsPhotoprofileOrchestrator;
            _apiKeyOptions = apiKeyOptions;
        }

        public override async Task<RemoveTicketsPhotoprofileResponse> RemoveTicketsPhotoprofile(
            RemoveTicketsPhotoprofileRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);

            if (!Guid.TryParse(request.PhotoProfileId, out var photoProfileId))
            {
                return new RemoveTicketsPhotoprofileResponse
                {
                    IsSuccess = false,
                    Message = "Некорректный идентификатор фото-профиля",
                };
            }

            var result = await _removeTicketsPhotoprofileOrchestrator.RemoveTickets(
                photoProfileId,
                context.CancellationToken);

            return result.Value;
        }
    }
}
