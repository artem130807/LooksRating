using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Query.GetPendingSeasonRolloverNotifications
{
    public sealed record GetPendingSeasonRolloverNotificationsQuery(int Limit = 0)
        : IRequest<IReadOnlyList<PendingSeasonRolloverNotificationResponse>>;
}
