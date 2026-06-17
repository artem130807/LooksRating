using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Query.GetUserStats
{
    public sealed record GetUserStatsQuery(long TelegramId) : IRequest<Result<GetUserStatsResponse>>;
}
