using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.Orchestrators;

public sealed class ListWritingOffSparksCitiesOrchestrator : IListWritingOffSparksCitiesOrchestrator
{
    private readonly IWritingOffSparksRepository _writingOffSparksRepository;

    public ListWritingOffSparksCitiesOrchestrator(IWritingOffSparksRepository writingOffSparksRepository)
    {
        _writingOffSparksRepository = writingOffSparksRepository;
    }

    public async Task<Result<ListWritingOffSparksCitiesResponse>> ListCitiesAsync(
        CancellationToken cancellationToken)
    {
        var cities = await _writingOffSparksRepository.GetCitiesWithPendingWritingsOffSparks();

        return Result.Success(new ListWritingOffSparksCitiesResponse
        {
            Success = true,
            Message = "Список городов получен",
            Cities = { cities },
        });
    }
}
