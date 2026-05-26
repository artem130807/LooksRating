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
        Task<bool> ExistsByUserAndPhoto(Guid userId, Guid photoUserId);
        Task<HashSet<Guid>> GetReviewedPhotoUserIdsAsync(
            Guid userId,
            IReadOnlyCollection<Guid> photoUserIds,
            CancellationToken cancellationToken = default);
        Task<Review?> GetByUserAndPhotoAsync(Guid userId, Guid photoUserId, CancellationToken cancellationToken = default);
    }
}