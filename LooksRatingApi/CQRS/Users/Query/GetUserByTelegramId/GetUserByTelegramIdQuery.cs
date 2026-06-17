using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Query.GetUserByTelegramId
{
    public sealed record GetUserByTelegramIdQuery(long TelegramId) : IRequest<Result<GetUserByTelegramIdResponse>>;
}
