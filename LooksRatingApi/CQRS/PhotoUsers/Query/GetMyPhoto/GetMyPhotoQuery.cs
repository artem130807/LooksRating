using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto
{
    public sealed record GetMyPhotoQuery(long TelegramId) : IRequest<Result<GetMyPhotoResponse>>;
}
