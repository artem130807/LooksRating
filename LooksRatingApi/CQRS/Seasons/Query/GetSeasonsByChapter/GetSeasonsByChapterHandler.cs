using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Query.GetSeasonsByChapter
{
    public sealed class GetSeasonsByChapterHandler
        : IRequestHandler<GetSeasonsByChapterQuery, Result<List<SeasonSummaryResponse>>>
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly IListSeasonsRepository _listSeasonsRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public GetSeasonsByChapterHandler(
            ISeasonRepository seasonRepository,
            IListSeasonsRepository listSeasonsRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _seasonRepository = seasonRepository;
            _listSeasonsRepository = listSeasonsRepository;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<Result<List<SeasonSummaryResponse>>> Handle(
            GetSeasonsByChapterQuery query,
            CancellationToken cancellationToken)
        {
            if (query.ListSeasonsId == Guid.Empty)
                return Result.Failure<List<SeasonSummaryResponse>>("Идентификатор главы обязателен");

            var chapter = await _listSeasonsRepository.GetByIdAsync(query.ListSeasonsId, cancellationToken);
            if (chapter is null)
                return Result.Failure<List<SeasonSummaryResponse>>("Глава не найдена");

            var seasons = await _seasonRepository.GetByListSeasonsIdAsync(
                query.ListSeasonsId,
                query.IncludeClosed,
                cancellationToken);

            var profileCounts = await _photoProfileRepository.GetParticipantCountsBySeasonIdsAsync(
                seasons.Select(s => s.Id),
                cancellationToken);

            var result = seasons
                .Select(s => SeasonCatalogMapping.ToSeasonSummary(s, profileCounts))
                .ToList();

            return Result.Success(result);
        }
    }
}
