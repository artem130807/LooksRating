using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly LooksRatingDbContext _context;
        public UserSessionRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task<UserSession?> GetByTelegramId(long telegramId)
        {
            return await _context.UserSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TelegramId == telegramId);
        }

        public async Task Create(UserSession session)
        {
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task Update(UserSession session)
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync();
        }
    }
}