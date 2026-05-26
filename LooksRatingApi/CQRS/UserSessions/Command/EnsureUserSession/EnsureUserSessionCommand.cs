using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.UserSessions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Command.EnsureUserSession
{
    public sealed record EnsureUserSessionCommand(long TelegramId, BotSessionState? InitialState)
        : IRequest<Result<UserSessionResponse>>;
}
