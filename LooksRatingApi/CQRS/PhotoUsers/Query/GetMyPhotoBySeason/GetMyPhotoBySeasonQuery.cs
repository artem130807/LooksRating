using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhotoBySeason
{
    public sealed record GetMyPhotoBySeasonQuery(long TelegramId, Guid SeasonId)
        : IRequest<Result<GetMyPhotoResponse>>;
}
