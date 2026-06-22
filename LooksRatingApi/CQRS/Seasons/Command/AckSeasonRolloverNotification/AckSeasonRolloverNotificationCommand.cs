using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Command.AckSeasonRolloverNotification
{
    public sealed record AckSeasonRolloverNotificationCommand(
        string EventId,
        IReadOnlyList<long> RecipientTelegramIds) : IRequest<Result<string>>;
}
