using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.WritingOffSparks;

public interface IGetWritingsOffSparksOrchestrator
{
    Task<Result<GetWritingsOffSparksResponse>> GetByCityAsync(
        string city,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
