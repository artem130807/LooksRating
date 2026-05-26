using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetLatestListSeason
{
    public sealed class GetLatestListSeasonHandler
        : IRequestHandler<GetLatestListSeasonQuery, Result<ListSeasonResponse>>
    {
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetLatestListSeasonHandler(
            IListSeasonsRepository listSeasonsRepository,
            ISeasonRepository seasonRepository)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<ListSeasonResponse>> Handle(
            GetLatestListSeasonQuery query,
            CancellationToken cancellationToken)
        {
            var list = await _listSeasonsRepository.GetLatestAsync(query.IncludeSeasons, cancellationToken);
            if (list is null)
                return Result.Failure<ListSeasonResponse>("Глава не найдена");

            Dictionary<Guid, int>? photoCounts = null;
            if (query.IncludeSeasons && list.Seasons.Count > 0)
            {
                photoCounts = await _seasonRepository.GetPhotoCountsBySeasonIdsAsync(
                    list.Seasons.Select(s => s.Id),
                    cancellationToken);
            }

            return Result.Success(SeasonCatalogMapping.ToListSeasonResponse(
                list,
                query.IncludeSeasons,
                photoCounts));
        }
    }
}
