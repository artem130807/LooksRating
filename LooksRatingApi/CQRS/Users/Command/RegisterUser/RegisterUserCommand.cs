using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser
{
    public sealed record RegisterUserCommand(
        long TelegramId,
        string? TelegramUsername,
        bool UseTelegramUsernameAsDisplay,
        string? Name,
        string? Link) : IRequest<Result<RegisterUserResult>>;
}
