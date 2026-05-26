using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.DeleteUserAccount
{
    public sealed record DeleteUserAccountCommand(long TelegramId)
        : IRequest<Result<Unit>>;
}
