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

        public async Task<Review> GetReviewById(Guid Id)
        {
            return await _context.Reviews.FindAsync(Id);
        }

        public async Task<List<Review>> GetReviewsByTelegramId(long? telegramId)
        {
            return await _context.Reviews.Include(x => x.User).Where(x => x.User.TelegramId == telegramId).ToListAsync();
        }

        public async Task Update(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
        }
    }
}