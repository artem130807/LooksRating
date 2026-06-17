using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestVipPhotos
{
    public sealed record GetTheBestVipPhotosQuery(
        long TelegramId,
        GenderEnum Gender,
        int Age) : IRequest<Result<List<GetTheBestVipPhotosResponse>>>;
}
