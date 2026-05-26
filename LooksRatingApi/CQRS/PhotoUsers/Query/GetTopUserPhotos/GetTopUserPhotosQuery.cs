using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed record GetTopUserPhotosQuery(
        long TelegramId,
        GenderEnum Gender,
        int Age,
        Guid? SeasonId,
        int Page,
        int PageSize) : IRequest<Result<GetTopUserPhotosPagedResponse>>;
}
