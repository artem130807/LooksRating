using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts
{
    public interface IDebitedSparksOrchestrator
    {
        Task<Result<DebitedSparksResponse>> DebitedSparks(
            long telegramId,
            int starsCount,
            string? idempotencyKey,
            CancellationToken cancellationToken);
    }
}