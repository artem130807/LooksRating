using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.TheBestWeekContracts;

namespace LooksRatingApi.Repositories
{
    public class TheBestWeekRepository:ITheBestWeekRepository
    {
        private readonly LooksRatingDbContext _context;
        public TheBestWeekRepository(LooksRatingDbContext context)
        {
            _context = context;
        }
    }
}