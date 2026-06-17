using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.GrpcService
{
    public sealed class RollBackDebitedSparksGrpcService : RollBackDebitedSparksService.RollBackDebitedSparksServiceBase
    {
        private readonly IRollBackDebitedSparksOrchestrator _rollBackDebitedSparksOrchestrator;

        public RollBackDebitedSparksGrpcService(IRollBackDebitedSparksOrchestrator rollBackDebitedSparksOrchestrator)
        {
            _rollBackDebitedSparksOrchestrator = rollBackDebitedSparksOrchestrator;
        }

        public override async Task<RollBackDebitedSparksResponse> RollBackDebitedSparks(
            RollBackDebitedSparksRequest request,
            ServerCallContext context)
        {
            var result = await _rollBackDebitedSparksOrchestrator.RollBackDebitedSparks(
                request.TelegramId,
                request.StarsCount,
                request.Reason,
                context.CancellationToken);

            if (result.IsFailure)
            {
                return new RollBackDebitedSparksResponse
                {
                    Success = false,
                    Message = result.Error
                };
            }

            return result.Value;
        }
    }
}
