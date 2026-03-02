using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class TheBestWeekRepository:ITheBestWeekRepository
    {
        private readonly LooksRatingDbContext _context;
        public TheBestWeekRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task Create(TheBestWeek theBestWeek)
        {
            _context.TheBestWeeks.Add(theBestWeek);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid Id)
        {
            await _context.TheBestWeeks.Where(x => x.Id == Id).ExecuteDeleteAsync();
        }

        public async Task<List<TheBestWeek>> GetTheBestWeekByCity(string city)
        {
            return await _context.TheBestWeeks
                .Where(x => x.City.Value == city)
                .ToListAsync();
        }

        public async Task Update(TheBestWeek theBestWeek)
        {
            _context.TheBestWeeks.Update(theBestWeek);
            await _context.SaveChangesAsync();
        }
    }
}