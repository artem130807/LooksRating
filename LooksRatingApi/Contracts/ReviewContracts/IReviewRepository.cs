using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.ReviewContracts
{
    public interface IReviewRepository
    {
        Task Create(Review review);
        Task Delete(Guid Id);
        Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task Update(Review review);
        Task<Review?> GetReviewById(Guid Id);
        Task<List<Review>> GetReviewsByTelegramId(long telegramId);
        Task<bool> ExistsByUserAndProfile(Guid userId, Guid photoProfileId);
        Task<Review?> GetByUserAndProfileAsync(Guid userId, Guid photoProfileId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetReviewerUserIdsByPhotoProfileIdAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default);

        Task DeleteByPhotoProfileIdAsync(Guid photoProfileId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Review>> GetReviewersForProfileCycleAsync(
            Guid photoProfileId,
            int cycleNumber,
            int reviewsPerCycle,
            CancellationToken cancellationToken = default);
    }
}