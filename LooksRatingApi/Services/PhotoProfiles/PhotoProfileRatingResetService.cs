using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using LooksRatingApi.Models;
using LooksRatingApi.Services.PhotoProfiles;
using StackExchange.Redis;

namespace LooksRatingApi.Services.PhotoProfiles
{
    public sealed class PhotoProfileRatingResetService : IPhotoProfileRatingResetService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IReviewMilestoneNotificationRepository _milestoneNotificationRepository;
        private readonly IPhotoRatingCacheService _photoRatingCacheService;
        private readonly IDatabase _redis;

        public PhotoProfileRatingResetService(
            IReviewRepository reviewRepository,
            IPhotoUserRepository photoUserRepository,
            IReviewMilestoneNotificationRepository milestoneNotificationRepository,
            IPhotoRatingCacheService photoRatingCacheService,
            IConnectionMultiplexer redis)
        {
            _reviewRepository = reviewRepository;
            _photoUserRepository = photoUserRepository;
            _milestoneNotificationRepository = milestoneNotificationRepository;
            _photoRatingCacheService = photoRatingCacheService;
            _redis = redis.GetDatabase();
        }

        public async Task<IReadOnlyList<Guid>> ResetDatabaseAsync(
            PhotoProfile profile,
            CancellationToken cancellationToken = default)
        {
            var reviewerUserIds = await _reviewRepository.GetReviewerUserIdsByPhotoProfileIdAsync(
                profile.Id,
                cancellationToken);

            profile.ResetRatings();

            await _milestoneNotificationRepository.DeletePendingByPhotoProfileIdAsync(
                profile.Id,
                cancellationToken);
            await _reviewRepository.DeleteByPhotoProfileIdAsync(profile.Id, cancellationToken);
            await _photoUserRepository.ResetLegacyRatingsForProfileAsync(profile.Id, cancellationToken);

            return reviewerUserIds;
        }

        public async Task ResetCacheAsync(
            PhotoProfile profile,
            PhotoProfileNomination previousNomination,
            IReadOnlyCollection<Guid> reviewerUserIds,
            CancellationToken cancellationToken = default)
        {
            await _photoRatingCacheService.ClearRatedMarkersForProfileAsync(
                profile.Id,
                profile.SeasonId,
                reviewerUserIds,
                cancellationToken);

            await _photoRatingCacheService.ResetProfileRatingAsync(
                profile.Id,
                profile.SeasonId,
                previousNomination.City,
                profile.CityNomination.Value ?? string.Empty,
                cancellationToken);

            await _redis.KeyDeleteAsync(ReviewRedisKeys.SequenceCount(profile.Id));
        }
    }
}
