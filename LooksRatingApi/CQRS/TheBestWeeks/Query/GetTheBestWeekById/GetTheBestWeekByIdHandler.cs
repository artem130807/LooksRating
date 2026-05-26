using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Services;
using MediatR;
using System.Text.Json;

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

            var photos = JsonSerializer.Deserialize<List<LooksRatingApi.Models.PhotoUser>>(week.SnapshotJson) ?? [];

            var response = new GetTheBestWeekByIdResponse
            {
                Id = week.Id,
                City = week.City,
                Year = week.Year,
                WeekOfYear = week.WeekOfYear,
                Week = week.Week,
                CreatedDate = week.CreatedDate,
                Photos = photos
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.RatingCount)
                    .Select(p => new GetTheBestWeekByIdPhotoItemResponse
                    {
                        Id = p.Id,
                        TelegramFileId = p.TelegramFileId,
                        Rating = p.Rating,
                        RatingCount = p.RatingCount,
                        Rank = p.Rank.ToString(),
                        DisplayName = UserPublicDisplayName.Resolve(p.User),
                        AgeNomination = p.AgeNomination,
                        GenderNomination = p.GenderNomination
                    })
                    .ToList()
            };

            return Result.Success(response);
        }
    }
}
