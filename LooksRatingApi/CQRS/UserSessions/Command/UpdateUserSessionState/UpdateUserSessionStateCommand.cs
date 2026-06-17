using CSharpFunctionalExtensions;
using LooksRatingApi.CQRS.UserSessions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Command.UpdateUserSessionState
{
    public sealed record UpdateUserSessionStateCommand(long TelegramId, BotSessionState State)
        : IRequest<Result<UserSessionResponse>>;
}
