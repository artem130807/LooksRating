using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.WritingOffSparks;

public interface IUpdateStatusWritingOffSparksOrchestrator
{
    Task<Result<UpdateStatusWritingOffSparksResponse>> UpdateStatusAsync(
        Guid writingOffSparksId,
        Enums.OutputStatusEnum status,
        CancellationToken cancellationToken);
}
