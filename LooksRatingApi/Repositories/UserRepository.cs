using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Filters;
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

        public async Task AddCountInTop(List<long> ids)
        {
            await _context.Users.Where(u => ids.Contains(u.TelegramId))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.CountInTop, u => u.CountInTop + 1));
        }

        public async Task Create(User user)
        {
            _context.Users.Add(user);
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

        public async Task<PagedResult<long>> GetTelegramIdsPagedAsync(
            int page,
            int pageSize,
            bool onlyUnsubscribedChannel = false,
            CancellationToken cancellationToken = default)
        {
            var normalizedPage = Math.Max(page, 1);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 500);

            var query = _context.Users.AsNoTracking();
            if (onlyUnsubscribedChannel)
            {
                query = query.Where(user => !user.IssubscribeChannel);
            }

            var telegramIds = query
                .OrderBy(user => user.TelegramId)
                .Select(user => user.TelegramId);

            return await telegramIds.ToPagedAsync(
                new PageParams
                {
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                });
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task Update(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}