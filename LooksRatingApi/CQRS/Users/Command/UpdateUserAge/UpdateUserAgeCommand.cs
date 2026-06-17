using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserAge
{
    public sealed record UpdateUserAgeCommand(long TelegramId, int Age) : IRequest<Result<string>>;
}
