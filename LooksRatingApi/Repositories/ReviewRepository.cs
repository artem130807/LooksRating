using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class ReviewRepository:IReviewRepository
    {
        private readonly LooksRatingDbContext _context;
        public ReviewRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task Create(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid Id)
        {
            await _context.Reviews.Where(x => x.Id == Id).ExecuteDeleteAsync();
        }

        public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _context.Reviews
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<Review?> GetReviewById(Guid Id)
        {
            return await _context.Reviews.FindAsync(Id);
        }

        public async Task<List<Review>> GetReviewsByTelegramId(long telegramId)
        {
            return await _context.Reviews.Include(x => x.User).Where(x => x.User.TelegramId == telegramId).ToListAsync();
        }

        public async Task<bool> ExistsByUserAndProfile(Guid userId, Guid photoProfileId)
        {
            return await _context.Reviews.AnyAsync(x => x.UserId == userId && x.PhotoProfileId == photoProfileId);
        }

        public async Task<Review?> GetByUserAndProfileAsync(
            Guid userId,
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(
                    x => x.UserId == userId && x.PhotoProfileId == photoProfileId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetReviewerUserIdsByPhotoProfileIdAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .Where(x => x.PhotoProfileId == photoProfileId)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteByPhotoProfileIdAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            await _context.Reviews
                .Where(x => x.PhotoProfileId == photoProfileId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Review>> GetReviewersForProfileCycleAsync(
            Guid photoProfileId,
            int cycleNumber,
            int reviewsPerCycle,
            CancellationToken cancellationToken = default)
        {
            var skip = Math.Max(0, (cycleNumber - 1) * reviewsPerCycle);

            return await _context.Reviews
                .Include(x => x.User)
                .Where(x => x.PhotoProfileId == photoProfileId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Skip(skip)
                .Take(reviewsPerCycle)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetRatedPhotoProfileIdsForSeasonAsync(
            Guid reviewerUserId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .Where(r => r.UserId == reviewerUserId)
                .Where(r => r.PhotoProfile.SeasonId == seasonId)
                .Select(r => r.PhotoProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task Update(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
        }
    }
}