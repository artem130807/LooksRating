using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserCity
{
    public sealed record UpdateUserCityCommand(long TelegramId, string City) : IRequest<Result<string>>;
}
