using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.WritingOffSparks;

public interface IListWritingOffSparksCitiesOrchestrator
{
    Task<Result<ListWritingOffSparksCitiesResponse>> ListCitiesAsync(CancellationToken cancellationToken);
}
