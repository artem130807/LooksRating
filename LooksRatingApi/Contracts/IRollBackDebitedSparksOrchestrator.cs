using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts
{
    public interface IRollBackDebitedSparksOrchestrator
    {
        Task<Result<RollBackDebitedSparksResponse>> RollBackDebitedSparks(
            long telegramId,
            int starsCount,
            string reason,
            CancellationToken cancellationToken);
    }
}