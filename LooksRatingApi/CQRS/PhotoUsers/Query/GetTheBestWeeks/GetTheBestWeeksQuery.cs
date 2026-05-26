using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed record GetTheBestWeeksQuery(
        long? TelegramId) : IRequest<Result<List<GetTheBestWeeksResponse>>>;
}
