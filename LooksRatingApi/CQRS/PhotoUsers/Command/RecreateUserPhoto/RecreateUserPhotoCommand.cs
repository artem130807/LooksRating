using CSharpFunctionalExtensions;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using MediatR;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public sealed record RecreateUserPhotoCommand(RecreateUserPhotoRequest Request)
        : IRequest<Result<SetUserPhotoResult>>;
}
