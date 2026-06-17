using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow
{
    public sealed record GetTheBestWeekPhotosNowQuery(
        long TelegramId,
        GenderEnum Gender,
        int Age) : IRequest<Result<List<GetTheBestWeekPhotosNowResponse>>>;
}
