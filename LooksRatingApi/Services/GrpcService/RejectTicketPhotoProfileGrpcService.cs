using Grpc.Core;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Infrastructure.Auth;
using LooksRatingApi.Infrastructure.Grpc;
using LooksRatingGrpc;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.GrpcService
{
    public sealed class RejectTicketPhotoProfileGrpcService : RejectTicketPhotoProfileService.RejectTicketPhotoProfileServiceBase
    {
        private readonly IRejectTicketPhotoProfileOrchestrator _rejectTicketPhotoProfileOrchestrator;
        private readonly IOptions<ApiKeyAuthOptions> _apiKeyOptions;

        public RejectTicketPhotoProfileGrpcService(
            IRejectTicketPhotoProfileOrchestrator rejectTicketPhotoProfileOrchestrator,
            IOptions<ApiKeyAuthOptions> apiKeyOptions)
        {
            _rejectTicketPhotoProfileOrchestrator = rejectTicketPhotoProfileOrchestrator;
            _apiKeyOptions = apiKeyOptions;
        }

        public override async Task<RejectTicketPhotoProfileResponse> RejectTicketPhotoProfile(
            RejectTicketPhotoProfileRequest request,
            ServerCallContext context)
        {
            GrpcRequestAuth.EnsureApiKey(context, _apiKeyOptions);

            if (!Guid.TryParse(request.TicketId, out var ticketId))
            {
                return new RejectTicketPhotoProfileResponse
                {
                    IsSuccess = false,
                    Message = "Некорректный идентификатор жалобы",
                };
            }

            var result = await _rejectTicketPhotoProfileOrchestrator.RejectTicket(
                ticketId,
                context.CancellationToken);

            return result.Value;
        }
    }
}
