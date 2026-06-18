using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.UserReferenceLink;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Query.GetUserReferenceLink
{
    public record GetUserReferenceLinkQuery(long telegramId) : IRequest<Result<UserReferenceLinkResponse>>;
}