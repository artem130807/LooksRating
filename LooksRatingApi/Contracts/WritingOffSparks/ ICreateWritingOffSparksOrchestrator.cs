using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.WritingOffSparks
{
    public interface ICreateWritingOffSparksOrchestrator
    {
        Task<Result<CreateWritingOffSparksResponse>> ConfirmedWriting(long telegramId, decimal sparksCount, string key, int starsCount, CancellationToken cancellationToken);
    }
}