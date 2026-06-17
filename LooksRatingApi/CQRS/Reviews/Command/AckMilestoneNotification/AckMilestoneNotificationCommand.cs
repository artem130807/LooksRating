using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Reviews.Command.AckMilestoneNotification
{
    public sealed record AckMilestoneNotificationCommand(Guid NotificationId) : IRequest<Result<string>>;
}
