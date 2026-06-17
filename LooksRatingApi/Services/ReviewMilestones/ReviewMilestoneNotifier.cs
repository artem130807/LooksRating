using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services.ReviewMilestones
{
    public sealed class ReviewMilestoneNotifier : IReviewMilestoneNotifier
    {
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IReviewMilestoneNotificationRepository _notificationRepository;
        private readonly ILogger<ReviewMilestoneNotifier> _logger;

        public ReviewMilestoneNotifier(
            IPhotoProfileRepository photoProfileRepository,
            IReviewMilestoneNotificationRepository notificationRepository,
            ILogger<ReviewMilestoneNotifier> logger)
        {
            _photoProfileRepository = photoProfileRepository;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task TryNotifyAsync(CreateReviewEvent reviewEvent, CancellationToken cancellationToken)
        {
            if (!reviewEvent.IsNewReview
                || reviewEvent.ReviewsCount != ReviewSequenceConstants.MaxReviewsCount)
            {
                return;
            }

            var photoProfile = await _photoProfileRepository.GetByIdAsync(
                reviewEvent.PhotoProfileId,
                cancellationToken);

            if (photoProfile?.User is null || photoProfile.User.TelegramId <= 0)
            {
                _logger.LogWarning(
                    "Skip review milestone notification: profile {PhotoProfileId} owner is missing",
                    reviewEvent.PhotoProfileId);
                return;
            }

            var cycleNumber = Math.Max(1, photoProfile.RatingCount / ReviewSequenceConstants.MaxReviewsCount);
            var notification = ReviewMilestoneNotification.CreatePending(
                photoProfile.Id,
                photoProfile.User.TelegramId,
                cycleNumber);

            var created = await _notificationRepository.TryAddPendingAsync(notification, cancellationToken);
            if (created)
            {
                _logger.LogInformation(
                    "Review milestone notification queued: profile={PhotoProfileId}, owner={OwnerTelegramId}, cycle={CycleNumber}",
                    photoProfile.Id,
                    photoProfile.User.TelegramId,
                    cycleNumber);
            }
        }
    }
}
