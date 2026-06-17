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
            return await GetByIdAsync(id);
        }

        public async Task<ListSeasons?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ListSeasons
                .AsNoTracking()
                .Include(l => l.Seasons)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public async Task<ListSeasons?> GetLatest(bool includeSeasons = true)
        {
            return await GetLatestAsync(includeSeasons);
        }

        public async Task<ListSeasons?> GetLatestAsync(
            bool includeSeasons = true,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ListSeasons.AsNoTracking().AsQueryable();

            if (includeSeasons)
                query = query.Include(l => l.Seasons);

            return await query
                .OrderByDescending(l => l.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ListSeasons?> GetPreviousToLatest()
        {
            return await _context.ListSeasons
                .OrderByDescending(l => l.CreatedDate)
                .Skip(1)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ListSeasons>> GetLists(bool includeSeasons = false)
        {
            return await GetListsAsync(includeSeasons);
        }

        public async Task<List<ListSeasons>> GetListsAsync(
            bool includeSeasons = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ListSeasons.AsNoTracking().AsQueryable();

            if (includeSeasons)
                query = query.Include(l => l.Seasons);

            return await query
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync(cancellationToken);
        }
    }
}

