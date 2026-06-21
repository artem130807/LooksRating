using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Filters;
using LooksRatingApi.Services.Grpc.Mapping;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.Orchestrators;

public sealed class GetWritingsOffSparksOrchestrator : IGetWritingsOffSparksOrchestrator
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly IWritingOffSparksRepository _writingOffSparksRepository;
    private readonly INormalizeCityNameService _normalizeCityNameService;

    public GetWritingsOffSparksOrchestrator(
        IWritingOffSparksRepository writingOffSparksRepository,
        INormalizeCityNameService normalizeCityNameService)
    {
        _writingOffSparksRepository = writingOffSparksRepository;
        _normalizeCityNameService = normalizeCityNameService;
    }

    public async Task<Result<GetWritingsOffSparksResponse>> GetByCityAsync(
        string city,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return Result.Success(new GetWritingsOffSparksResponse
            {
                Success = false,
                Message = "Город не указан",
            });
        }

        var normalizedCity = _normalizeCityNameService.Normalize(city);
        if (string.IsNullOrWhiteSpace(normalizedCity))
        {
            return Result.Success(new GetWritingsOffSparksResponse
            {
                Success = false,
                Message = "Город не указан",
            });
        }

        var resolvedPage = page > 0 ? page : DefaultPage;
        var resolvedPageSize = pageSize > 0 ? Math.Min(pageSize, MaxPageSize) : DefaultPageSize;

        var pageResult = await _writingOffSparksRepository.GetPendingWritingsOffSparks(
            new PageParams
            {
                Page = resolvedPage,
                PageSize = resolvedPageSize,
            },
            normalizedCity);

        var hasNextPage = resolvedPage * resolvedPageSize < pageResult.Count;

        return Result.Success(new GetWritingsOffSparksResponse
        {
            Success = true,
            Message = "Список списаний искр получен",
            TotalCount = pageResult.Count,
            Page = resolvedPage,
            PageSize = resolvedPageSize,
            HasNextPage = hasNextPage,
            Items = { pageResult.Data.Select(WritingOffSparksGrpcMapper.ToItem) },
        });
    }
}
