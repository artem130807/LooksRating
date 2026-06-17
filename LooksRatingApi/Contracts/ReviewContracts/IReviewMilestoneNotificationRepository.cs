using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.ReviewContracts
{
    public interface IReviewMilestoneNotificationRepository
    {
        Task<bool> TryAddPendingAsync(ReviewMilestoneNotification notification, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ReviewMilestoneNotification>> GetPendingAsync(int limit, CancellationToken cancellationToken = default);

        Task<ReviewMilestoneNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task MarkSentAsync(Guid id, CancellationToken cancellationToken = default);

        Task DeletePendingByPhotoProfileIdAsync(Guid photoProfileId, CancellationToken cancellationToken = default);
    }
}
