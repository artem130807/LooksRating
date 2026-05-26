using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LooksRatingDbContext _context;
        public UserRepository(LooksRatingDbContext context)
        {
            _context = context;
        }
        public async Task Create(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid Id)
        {
            await _context.Users.Where(x => x.Id == Id)
            .ExecuteDeleteAsync();
        }

        public async Task<User?> GetUserById(Guid Id)
        {
            return await _context.Users.FindAsync(Id);
        }

        public async Task<User?> GetUserByTelegramId(long TelegramId)
        {
            return await _context.Users
                .Include(x => x.RecomendationSettings)
                .FirstOrDefaultAsync(x => x.TelegramId == TelegramId);
        }

        // public async Task<int> CountTimesInTopAsync(Guid userId, CancellationToken cancellationToken = default)
        // {
        //     return await _context.PhotoUsers
        //         .AsNoTracking()
        //         .Where(p => p.UserId == userId)
        //         .SelectMany(p => p.TheBestWeeks)
        //         .CountAsync(cancellationToken);
        // }

        public async Task<List<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task Update(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}