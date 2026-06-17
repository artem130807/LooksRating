using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public sealed class ReviewMilestoneNotificationRepository : IReviewMilestoneNotificationRepository
    {
        private readonly LooksRatingDbContext _context;

        public ReviewMilestoneNotificationRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> TryAddPendingAsync(
            ReviewMilestoneNotification notification,
            CancellationToken cancellationToken = default)
        {
            var exists = await _context.ReviewMilestoneNotifications.AnyAsync(
                x => x.PhotoProfileId == notification.PhotoProfileId
                    && x.CycleNumber == notification.CycleNumber,
                cancellationToken);

            if (exists)
            {
                return false;
            }

            _context.ReviewMilestoneNotifications.Add(notification);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<IReadOnlyList<ReviewMilestoneNotification>> GetPendingAsync(
            int limit,
            CancellationToken cancellationToken = default)
        {
            return await _context.ReviewMilestoneNotifications
                .Where(x => x.Status == ReviewMilestoneNotificationStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<ReviewMilestoneNotification?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.ReviewMilestoneNotifications
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task MarkSentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.ReviewMilestoneNotifications
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity is null)
            {
                return;
            }

            entity.MarkSent();
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeletePendingByPhotoProfileIdAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            await _context.ReviewMilestoneNotifications
                .Where(x => x.PhotoProfileId == photoProfileId
                    && x.Status == ReviewMilestoneNotificationStatus.Pending)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
