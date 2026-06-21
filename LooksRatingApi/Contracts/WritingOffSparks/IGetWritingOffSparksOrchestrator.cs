using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.WritingOffSparks;

public interface IGetWritingOffSparksOrchestrator
{
    Task<Result<GetWritingOffSparksResponse>> GetByIdAsync(
        Guid writingOffSparksId,
        CancellationToken cancellationToken);
}
