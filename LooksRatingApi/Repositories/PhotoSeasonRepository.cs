using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.PhotoSeasonContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class PhotoSeasonRepository : IPhotoSeasonRepository
    {
        private readonly LooksRatingDbContext _context;

        public PhotoSeasonRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task Create(PhotoSeason photoSeason)
        {
            _context.PhotoSeasons.Add(photoSeason);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            await _context.PhotoSeasons
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync();
        }

        public async Task Update(PhotoSeason photoSeason)
        {
            _context.PhotoSeasons.Update(photoSeason);
            await _context.SaveChangesAsync();
        }

        public async Task<PhotoSeason?> GetById(Guid id)
        {
            return await _context.PhotoSeasons
                .Include(p => p.Season)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PhotoSeason?> GetBySeasonAndUser(Guid seasonId, Guid userId)
        {
            return await _context.PhotoSeasons
                .Include(p => p.Season)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.SeasonId == seasonId && p.UserId == userId);
        }

        public async Task<List<PhotoSeason>> GetBySeason(Guid seasonId)
        {
            return await _context.PhotoSeasons
                .Include(p => p.User)
                .Where(p => p.SeasonId == seasonId)
                .ToListAsync();
        }

        public async Task<List<PhotoSeason>> GetTopBySeason(Guid seasonId, int take)
        {
            return await _context.PhotoSeasons
                .Include(p => p.User)
                .Where(p => p.SeasonId == seasonId)
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.RatingCount)
                .Take(take)
                .ToListAsync();
        }
    }
}

