using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasons
{
    public sealed class GetListSeasonsHandler
        : IRequestHandler<GetListSeasonsQuery, Result<List<ListSeasonResponse>>>
    {
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public GetListSeasonsHandler(
            IListSeasonsRepository listSeasonsRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<Result<List<ListSeasonResponse>>> Handle(
            GetListSeasonsQuery query,
            CancellationToken cancellationToken)
        {
            var lists = await _listSeasonsRepository.GetListsAsync(query.IncludeSeasons, cancellationToken);
            IReadOnlyDictionary<Guid, int>? profileCounts = null;

            if (query.IncludeSeasons)
            {
                var seasonIds = lists.SelectMany(l => l.Seasons).Select(s => s.Id);
                profileCounts = await _photoProfileRepository.GetParticipantCountsBySeasonIdsAsync(
                    seasonIds,
                    cancellationToken);
            }

            var result = lists
                .Select(l => SeasonCatalogMapping.ToListSeasonResponse(l, query.IncludeSeasons, profileCounts))
                .ToList();

            return Result.Success(result);
        }
    }
}
