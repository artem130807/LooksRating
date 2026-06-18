using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.UserReferenceLink;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Command.CreateUserReferenceLink
{
    public record CreateUserReferenceLinkCommand(long telegramId) : IRequest<Result<UserReferenceLinkResponse>>;
}