using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.ListSeasons.Query.GetListSeasonById
{
    public sealed class GetListSeasonByIdHandler
        : IRequestHandler<GetListSeasonByIdQuery, Result<ListSeasonResponse>>
    {
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetListSeasonByIdHandler(
            IListSeasonsRepository listSeasonsRepository,
            ISeasonRepository seasonRepository)
        {
            _listSeasonsRepository = listSeasonsRepository;
            _seasonRepository = seasonRepository;
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

            var photoCounts = await _seasonRepository.GetPhotoCountsBySeasonIdsAsync(
                list.Seasons.Select(s => s.Id),
                cancellationToken);

            return Result.Success(SeasonCatalogMapping.ToListSeasonResponse(list, true, photoCounts));
        }
    }
}
