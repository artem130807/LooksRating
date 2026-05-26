using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasons
{
    public sealed class GetListSeasonsHandler
        : IRequestHandler<GetListSeasonsQuery, Result<List<ListSeasonResponse>>>
    {
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetListSeasonsHandler(
            IListSeasonsRepository listSeasonsRepository,
            ISeasonRepository seasonRepository)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<List<ListSeasonResponse>>> Handle(
            GetListSeasonsQuery query,
            CancellationToken cancellationToken)
        {
            var lists = await _listSeasonsRepository.GetListsAsync(query.IncludeSeasons, cancellationToken);
            Dictionary<Guid, int>? photoCounts = null;

            if (query.IncludeSeasons)
            {
                var seasonIds = lists.SelectMany(l => l.Seasons).Select(s => s.Id);
                photoCounts = await _seasonRepository.GetPhotoCountsBySeasonIdsAsync(seasonIds, cancellationToken);
            }

            var result = lists
                .Select(l => SeasonCatalogMapping.ToListSeasonResponse(l, query.IncludeSeasons, photoCounts))
                .ToList();

            return Result.Success(result);
        }
    }
}
