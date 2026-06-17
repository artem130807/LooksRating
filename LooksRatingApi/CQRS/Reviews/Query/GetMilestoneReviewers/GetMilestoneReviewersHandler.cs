using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.Reviews.Query.GetMilestoneReviewers
{
    public sealed class GetMilestoneReviewersHandler
        : IRequestHandler<GetMilestoneReviewersQuery, Result<GetMilestoneReviewersResponse>>
    {
        private readonly IReviewMilestoneNotificationRepository _notificationRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;

        public GetMilestoneReviewersHandler(
            IReviewMilestoneNotificationRepository notificationRepository,
            IReviewRepository reviewRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _notificationRepository = notificationRepository;
            _reviewRepository = reviewRepository;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<Result<GetMilestoneReviewersResponse>> Handle(
            GetMilestoneReviewersQuery request,
            CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification is null)
            {
                return Result.Failure<GetMilestoneReviewersResponse>("ReviewMilestoneNotificationNotFound");
            }

            var ratedProfile = await _photoProfileRepository.GetByIdAsync(
                notification.PhotoProfileId,
                cancellationToken);
            if (ratedProfile is null)
            {
                return Result.Failure<GetMilestoneReviewersResponse>("PhotoProfileNotFound");
            }

            var reviews = await _reviewRepository.GetReviewersForProfileCycleAsync(
                notification.PhotoProfileId,
                notification.CycleNumber,
                ReviewSequenceConstants.MaxReviewsCount,
                cancellationToken);

            var reviewers = new List<MilestoneReviewerItem>();
            foreach (var review in reviews.Where(x => x.User is not null))
            {
                var reviewerProfile = await _photoProfileRepository.GetByUserAndSeasonAsync(
                    review.UserId,
                    ratedProfile.SeasonId,
                    cancellationToken);

                reviewers.Add(new MilestoneReviewerItem
                {
                    ReviewerUserId = review.UserId,
                    ReviewerTelegramId = review.User.TelegramId,
                    ReviewerPhotoProfileId = reviewerProfile?.Id,
                    DisplayName = UserPublicDisplayName.Resolve(review.User),
                    Rating = review.Rating
                });
            }

            return Result.Success(new GetMilestoneReviewersResponse
            {
                NotificationId = notification.Id,
                PhotoProfileId = notification.PhotoProfileId,
                Reviewers = reviewers
            });
        }
    }
}
