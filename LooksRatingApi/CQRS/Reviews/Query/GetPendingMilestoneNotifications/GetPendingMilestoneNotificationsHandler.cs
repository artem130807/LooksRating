using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Infrastructure.SendUserReview;
using MediatR;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.CQRS.Reviews.Query.GetPendingMilestoneNotifications
{
    public sealed class GetPendingMilestoneNotificationsHandler
        : IRequestHandler<GetPendingMilestoneNotificationsQuery, IReadOnlyList<PendingMilestoneNotificationResponse>>
    {
        private readonly IReviewMilestoneNotificationRepository _repository;
        private readonly ReviewMilestoneNotificationOptions _options;

        public GetPendingMilestoneNotificationsHandler(
            IReviewMilestoneNotificationRepository repository,
            IOptions<ReviewMilestoneNotificationOptions> options)
        {
            _repository = repository;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<PendingMilestoneNotificationResponse>> Handle(
            GetPendingMilestoneNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                return Array.Empty<PendingMilestoneNotificationResponse>();
            }

            var limit = request.Limit > 0
                ? Math.Min(request.Limit, _options.PendingBatchSize)
                : _options.PendingBatchSize;

            var pending = await _repository.GetPendingAsync(limit, cancellationToken);
            return pending
                .Select(x => new PendingMilestoneNotificationResponse
                {
                    Id = x.Id,
                    PhotoProfileId = x.PhotoProfileId,
                    OwnerTelegramId = x.OwnerTelegramId,
                    CycleNumber = x.CycleNumber,
                    CreatedAt = x.CreatedAt
                })
                .ToList();
        }
    }
}
