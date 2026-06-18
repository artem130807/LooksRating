using Grpc.Core;
using LooksRatingApi.Contracts;
using LooksRatingGrpc;
using DomainBonusStatus = LooksRatingApi.Contracts.ChannelSubscribeBonusStatus;

namespace LooksRatingApi.Services.GrpcService
{
    public sealed class CurrentSparksForUserGrpcService
        : CurrentSparksForUserService.CurrentSparksForUserServiceBase
    {
        private readonly ICurrentSparksForUserOrchestrator _orchestrator;

        public CurrentSparksForUserGrpcService(ICurrentSparksForUserOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public override async Task<CurrentSparksForUserResponse> CurrentSparksForUser(
            CurrentSparksForUserRequest request,
            ServerCallContext context)
        {
            var result = await _orchestrator.ProcessAsync(
                request.TelegramId,
                request.Credit,
                context.CancellationToken);

            return new CurrentSparksForUserResponse
            {
                Success = result.Success,
                Message = result.Message,
                Status = MapStatus(result.Status),
            };
        }

        private static LooksRatingGrpc.ChannelSubscribeBonusStatus MapStatus(DomainBonusStatus domainStatus) =>
            domainStatus switch
            {
                DomainBonusStatus.Credited => LooksRatingGrpc.ChannelSubscribeBonusStatus.Credited,
                DomainBonusStatus.AlreadyCredited => LooksRatingGrpc.ChannelSubscribeBonusStatus.AlreadyCredited,
                DomainBonusStatus.UserNotFound => LooksRatingGrpc.ChannelSubscribeBonusStatus.UserNotFound,
                DomainBonusStatus.Failed => LooksRatingGrpc.ChannelSubscribeBonusStatus.Failed,
                DomainBonusStatus.Eligible => LooksRatingGrpc.ChannelSubscribeBonusStatus.Eligible,
                _ => LooksRatingGrpc.ChannelSubscribeBonusStatus.Unknown,
            };
    }
}
