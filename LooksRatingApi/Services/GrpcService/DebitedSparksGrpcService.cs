using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.GrpcService
{
    public class DebitedSparksGrpcService : DebitedSparksService.DebitedSparksServiceBase
    {
        private readonly IDebitedSparksOrchestrator _debitedSparksOrchestrator;

        public DebitedSparksGrpcService(IDebitedSparksOrchestrator debitedSparksOrchestrator)
        {
            _debitedSparksOrchestrator = debitedSparksOrchestrator;
        }
        public override async Task<DebitedSparksResponse> DebitedSparks(DebitedSparksRequest request, ServerCallContext context)
        {
            var result = await _debitedSparksOrchestrator.DebitedSparks(
                request.TelegramId,
                request.SparksCount,
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
