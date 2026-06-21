using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Services.Grpc.Mapping;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.Orchestrators;

public sealed class GetWritingOffSparksOrchestrator : IGetWritingOffSparksOrchestrator
{
    private readonly IWritingOffSparksRepository _writingOffSparksRepository;

    public GetWritingOffSparksOrchestrator(IWritingOffSparksRepository writingOffSparksRepository)
    {
        _writingOffSparksRepository = writingOffSparksRepository;
    }

    public async Task<Result<GetWritingOffSparksResponse>> GetByIdAsync(
        Guid writingOffSparksId,
        CancellationToken cancellationToken)
    {
        var writingOffSparks = await _writingOffSparksRepository.GetById(writingOffSparksId);
        if (writingOffSparks is null)
        {
            return Result.Success(new GetWritingOffSparksResponse
            {
                Success = false,
                Message = "Списание искр не найдено",
            });
        }

        return Result.Success(new GetWritingOffSparksResponse
        {
            Success = true,
            Message = "Списание искр найдено",
            Item = WritingOffSparksGrpcMapper.ToItem(writingOffSparks),
        });
    }
}
