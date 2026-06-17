using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasonById
{
    public sealed class GetListSeasonByIdHandler
        : IRequestHandler<GetListSeasonByIdQuery, Result<ListSeasonResponse>>
    {
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public GetListSeasonByIdHandler(
            IListSeasonsRepository listSeasonsRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<Result<ListSeasonResponse>> Handle(
            GetListSeasonByIdQuery query,
            CancellationToken cancellationToken)
        {
            if (query.Id == Guid.Empty)
                return Result.Failure<ListSeasonResponse>("Идентификатор главы обязателен");

            var list = await _listSeasonsRepository.GetByIdAsync(query.Id, cancellationToken);
            if (list is null)
                return Result.Failure<ListSeasonResponse>("Глава не найдена");

            var profileCounts = await _photoProfileRepository.GetParticipantCountsBySeasonIdsAsync(
                list.Seasons.Select(s => s.Id),
                cancellationToken);

            return Result.Success(SeasonCatalogMapping.ToListSeasonResponse(list, true, profileCounts));
        }
    }
}
