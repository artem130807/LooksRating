using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.CQRS.Seasons;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Query.GetSeasonById
{
    public sealed class GetSeasonByIdHandler
        : IRequestHandler<GetSeasonByIdQuery, Result<SeasonResponse>>
    {
        private readonly ISeasonRepository _seasonRepository;

        public GetSeasonByIdHandler(ISeasonRepository seasonRepository)
        {
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<SeasonResponse>> Handle(
            GetSeasonByIdQuery query,
            CancellationToken cancellationToken)
        {
            if (query.Id == Guid.Empty)
                return Result.Failure<SeasonResponse>("Идентификатор сезона обязателен");

            var season = query.IncludeChapter
                ? await _seasonRepository.GetByIdWithChapterAsync(query.Id, cancellationToken)
                : await _seasonRepository.GetById(query.Id);

            if (season is null)
                return Result.Failure<SeasonResponse>("Сезон не найден");

            var photoCounts = await _seasonRepository.GetPhotoCountsBySeasonIdsAsync(
                new[] { season.Id },
                cancellationToken);

            var response = SeasonCatalogMapping.ToSeasonResponse(
                season,
                photoCounts.GetValueOrDefault(season.Id));

            if (query.IncludeChapter && season.ListSeasons is not null)
            {
                response.Chapter = SeasonCatalogMapping.ToListSeasonResponse(
                    season.ListSeasons,
                    false);
            }

            return Result.Success(response);
        }
    }
}
