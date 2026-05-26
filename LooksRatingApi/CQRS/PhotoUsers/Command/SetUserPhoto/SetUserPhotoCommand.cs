using CSharpFunctionalExtensions;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public sealed record SetUserPhotoCommand(
        SetUserPhotoRequest request) : IRequest<Result<SetUserPhotoResult>>;
}
