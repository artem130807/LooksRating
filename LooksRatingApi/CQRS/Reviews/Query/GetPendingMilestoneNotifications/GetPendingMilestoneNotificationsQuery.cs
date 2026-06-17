using MediatR;

namespace LooksRatingApi.CQRS.Reviews.Query.GetPendingMilestoneNotifications
{
    public sealed record GetPendingMilestoneNotificationsQuery(int Limit = 50)
        : IRequest<IReadOnlyList<PendingMilestoneNotificationResponse>>;
}
