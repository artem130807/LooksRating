using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.UserSessions;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Query.GetUserSession
{
    public sealed record GetUserSessionQuery(long TelegramId) : IRequest<Result<UserSessionResponse>>;
}
