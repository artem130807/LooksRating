using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.TopPhotoSeasonContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class TopPhotoSeasonRepository : ITopPhotoSeasonRepository
    {
        private readonly LooksRatingDbContext _context;

        public TopPhotoSeasonRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task Create(TopPhotoSeason topPhotoSeason)
        {
            _context.TopPhotoSeasons.Add(topPhotoSeason);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            await _context.TopPhotoSeasons
                .Where(t => t.Id == id)
                .ExecuteDeleteAsync();
        }

        public async Task Update(TopPhotoSeason topPhotoSeason)
        {
            _context.TopPhotoSeasons.Update(topPhotoSeason);
            await _context.SaveChangesAsync();
        }

        public async Task<TopPhotoSeason?> GetById(Guid id)
        {
            return await _context.TopPhotoSeasons
                .Include(t => t.PhotoSeason)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TopPhotoSeason?> GetByPhotoSeasonAndGender(Guid photoSeasonId, GenderEnum gender)
        {
            return await _context.TopPhotoSeasons
                .Include(t => t.PhotoSeason)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(t => t.PhotoSeasonId == photoSeasonId && t.GenderEnum == gender);
        }

        public async Task<List<TopPhotoSeason>> GetTopBySeason(Guid seasonId, string city, GenderEnum gender, int take)
        {
            return await _context.TopPhotoSeasons
                .Include(t => t.PhotoSeason)
                .ThenInclude(p => p.User)
                .Where(t =>
                    t.PhotoSeason.SeasonId == seasonId &&
                    t.GenderEnum == gender &&
                    t.City.Value == city)
                .OrderBy(t => t.Place)
                .Take(take)
                .ToListAsync();
        }
    }
}

