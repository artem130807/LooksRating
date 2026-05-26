using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.UserSessions;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Command.LinkUserSession
{
    public sealed record LinkUserSessionCommand(long TelegramId, Guid UserId)
        : IRequest<Result<UserSessionResponse>>;
}
