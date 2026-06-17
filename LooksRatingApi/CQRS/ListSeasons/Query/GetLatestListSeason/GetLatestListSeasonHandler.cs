using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetLatestListSeason
{
    public sealed class GetLatestListSeasonHandler
        : IRequestHandler<GetLatestListSeasonQuery, Result<ListSeasonResponse>>
    {
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public GetLatestListSeasonHandler(
            IListSeasonsRepository listSeasonsRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<Result<ListSeasonResponse>> Handle(
            GetLatestListSeasonQuery query,
            CancellationToken cancellationToken)
        {
            var list = await _listSeasonsRepository.GetLatestAsync(query.IncludeSeasons, cancellationToken);
            if (list is null)
                return Result.Failure<ListSeasonResponse>("Глава не найдена");

            IReadOnlyDictionary<Guid, int>? profileCounts = null;
            if (query.IncludeSeasons && list.Seasons.Count > 0)
            {
                profileCounts = await _photoProfileRepository.GetParticipantCountsBySeasonIdsAsync(
                    list.Seasons.Select(s => s.Id),
                    cancellationToken);
            }

            return Result.Success(SeasonCatalogMapping.ToListSeasonResponse(
                list,
                query.IncludeSeasons,
                profileCounts));
        }
    }
}
