using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Services;
using LooksRatingApi.Services.TheBestWeek;
using MediatR;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeekById
{
    public sealed class GetTheBestWeekByIdHandler
        : IRequestHandler<GetTheBestWeekByIdQuery, Result<GetTheBestWeekByIdResponse>>
    {
        private readonly ITheBestWeekRepository _theBestWeekRepository;

        public GetTheBestWeekByIdHandler(ITheBestWeekRepository theBestWeekRepository)
        {
            _theBestWeekRepository = theBestWeekRepository;
        }

        public async Task<Result<GetTheBestWeekByIdResponse>> Handle(
            GetTheBestWeekByIdQuery query,
            CancellationToken cancellationToken)
        {
            if (query.Id == Guid.Empty)
            {
                return Result.Failure<GetTheBestWeekByIdResponse>("Идентификатор недели обязателен");
            }

            var week = await _theBestWeekRepository.GetByIdAsync(query.Id, cancellationToken);
            if (week is null)
            {
                return Result.Failure<GetTheBestWeekByIdResponse>("Лучшая неделя не найдена");
            }

            var snapshotItems = TheBestWeekSnapshotSerializer.Deserialize(week.SnapshotJson);

            var response = new GetTheBestWeekByIdResponse
            {
                Id = week.Id,
                City = week.City,
                Year = week.Year,
                WeekOfYear = week.WeekOfYear,
                Week = week.Week,
                CreatedDate = week.CreatedDate,
                Photos = snapshotItems
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.RatingCount)
                    .Select(p => new GetTheBestWeekByIdPhotoItemResponse
                    {
                        Id = p.ProfileId,
                        TelegramFileId = p.TelegramFileIds.FirstOrDefault() ?? string.Empty,
                        Rating = p.Rating,
                        RatingCount = p.RatingCount,
                        Rank = p.Rank,
                        DisplayName = p.DisplayName,
                        AgeNomination = p.AgeNomination,
                        GenderNomination = p.GenderNomination
                    })
                    .ToList()
            };

            return Result.Success(response);
        }
    }
}
