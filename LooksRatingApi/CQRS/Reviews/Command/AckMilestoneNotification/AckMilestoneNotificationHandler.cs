using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.ReviewContracts;
using MediatR;

namespace LooksRatingApi.CQRS.Reviews.Command.AckMilestoneNotification
{
    public sealed class AckMilestoneNotificationHandler
        : IRequestHandler<AckMilestoneNotificationCommand, Result<string>>
    {
        private readonly IReviewMilestoneNotificationRepository _repository;

        public AckMilestoneNotificationHandler(IReviewMilestoneNotificationRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<string>> Handle(
            AckMilestoneNotificationCommand request,
            CancellationToken cancellationToken)
        {
            var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification is null)
            {
                return Result.Failure<string>("ReviewMilestoneNotificationNotFound");
            }

            await _repository.MarkSentAsync(notification.Id, cancellationToken);
            return Result.Success("ok");
        }
    }
}
