using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosId;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query
{
    public sealed class GetTheBestWeekPhotosIdHandler : IRequestHandler<GetTheBestWeekPhotosIdQuery, Result<List<long>>>
    {
        private readonly ITheBestWeekTopStatsService _topStatsService;

        public GetTheBestWeekPhotosIdHandler(ITheBestWeekTopStatsService topStatsService)
        {
            _topStatsService = topStatsService;
        }

        public async Task<Result<List<long>>> Handle(
            GetTheBestWeekPhotosIdQuery request,
            CancellationToken cancellationToken)
        {
            var ids = await _topStatsService.GetCurrentWeekTopTelegramIdsAsync(cancellationToken);
            if (ids.Count == 0)
            {
                return Result.Failure<List<long>>("Список айди пуст");
            }

            return Result.Success(ids);
        }
    }
}
