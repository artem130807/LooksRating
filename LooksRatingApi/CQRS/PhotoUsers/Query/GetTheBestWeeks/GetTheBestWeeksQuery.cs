using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using MediatR;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed record GetTheBestWeeksQuery(
        long? TelegramId,
        GenderEnum Gender,
        int Age) : IRequest<Result<List<GetTheBestWeeksResponse>>>;
}
