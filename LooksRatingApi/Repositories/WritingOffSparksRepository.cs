using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Enums;
using LooksRatingApi.Filters;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class WritingOffSparksRepository : IWritingOffSparksRepository
    {
        private readonly LooksRatingDbContext _context;

        public WritingOffSparksRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public Task Add(WritingOffSparks writingOffSparks)
        {
            _context.WritingOffSparks.Add(writingOffSparks);
            return Task.CompletedTask;
        }

        public async Task<WritingOffSparks?> GetById(Guid id)
        {
            return await _context.WritingOffSparks
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<WritingOffSparks>> GetPendingWritingsOffSparks(
            PageParams pageParams,
            string city)
        {
            return await _context.WritingOffSparks
                .Include(x => x.User)
                .Where(x => x.City == city && x.Status == OutputStatusEnum.Pending)
                .OrderByDescending(x => x.CreatedAt)
                .ToPagedAsync(pageParams);
        }

        public async Task<List<string>> GetCitiesWithPendingWritingsOffSparks()
        {
            return await _context.WritingOffSparks
                .AsNoTracking()
                .Where(x => x.Status == OutputStatusEnum.Pending)
                .Select(x => x.City)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<WritingOffSparks?> GetByUserIdAndIdempotencyKey(Guid userId, string idempotencyKey)
        {
            return await _context.WritingOffSparks
                .FirstOrDefaultAsync(w => w.UserId == userId && w.IdempotencyKey == idempotencyKey);
        }
    }
}
