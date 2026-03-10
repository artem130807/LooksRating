using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.ListSeasonsContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class ListSeasonsRepository : IListSeasonsRepository
    {
        private readonly LooksRatingDbContext _context;

        public ListSeasonsRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task Create(ListSeasons listSeasons)
        {
            _context.ListSeasons.Add(listSeasons);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            await _context.ListSeasons
                .Where(l => l.Id == id)
                .ExecuteDeleteAsync();
        }

        public async Task Update(ListSeasons listSeasons)
        {
            _context.ListSeasons.Update(listSeasons);
            await _context.SaveChangesAsync();
        }

        public async Task<ListSeasons?> GetById(Guid id)
        {
            return await _context.ListSeasons
                .Include(l => l.Seasons)
                .ThenInclude(s => s.PhotoSeasons)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<ListSeasons?> GetLatest(bool includeSeasons = true)
        {
            var query = _context.ListSeasons.AsQueryable();

            if (includeSeasons)
            {
                query = query
                    .Include(l => l.Seasons)
                    .ThenInclude(s => s.PhotoSeasons);
            }

            return await query
                .OrderByDescending(l => l.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ListSeasons>> GetLists(bool includeSeasons = false)
        {
            var query = _context.ListSeasons.AsQueryable();

            if (includeSeasons)
            {
                query = query
                    .Include(l => l.Seasons)
                    .ThenInclude(s => s.PhotoSeasons);
            }

            return await query
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();
        }
    }
}

