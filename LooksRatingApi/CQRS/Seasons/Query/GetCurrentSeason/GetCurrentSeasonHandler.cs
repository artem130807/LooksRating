using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.CQRS.Seasons;
using LooksRatingApi.Models;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Query.GetCurrentSeason
{
    public sealed class GetCurrentSeasonHandler
        : IRequestHandler<GetCurrentSeasonQuery, Result<SeasonResponse>>
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public GetCurrentSeasonHandler(
            ISeasonRepository seasonRepository,
            IListSeasonsRepository listSeasonsRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _seasonRepository = seasonRepository;
            _listSeasonsRepository = listSeasonsRepository;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<Result<SeasonResponse>> Handle(
            GetCurrentSeasonQuery query,
            CancellationToken cancellationToken)
        {
            Season? season;

            if (query.ListSeasonsId.HasValue && query.ListSeasonsId.Value != Guid.Empty)
            {
                season = await _seasonRepository.GetCurrentByList(query.ListSeasonsId.Value);
            }
            else
            {
                var latestChapter = await _listSeasonsRepository.GetLatestAsync(false, cancellationToken);
                if (latestChapter is null)
                    return Result.Failure<SeasonResponse>("Глава не найдена");

                season = await _seasonRepository.GetCurrentByList(latestChapter.Id);
            }

            if (season is null)
                return Result.Failure<SeasonResponse>("Текущий сезон не найден");

            var profileCounts = await _photoProfileRepository.GetParticipantCountsBySeasonIdsAsync(
                new[] { season.Id },
                cancellationToken);

            var response = SeasonCatalogMapping.ToSeasonResponse(
                season,
                profileCounts.GetValueOrDefault(season.Id));

            return Result.Success(response);
        }
    }
}
