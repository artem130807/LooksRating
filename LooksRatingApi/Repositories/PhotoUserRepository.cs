using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class PhotoUserRepository : IPhotoUserRepository
    {
        private readonly LooksRatingDbContext _context;
        public PhotoUserRepository(LooksRatingDbContext context)
        {
            _context = context;
        }
        public async Task Create(PhotoUser photoUser)
        {
            _context.PhotoUsers.Add(photoUser);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid Id)
        {
            await _context.PhotoUsers.Where(x => x.Id == Id).ExecuteDeleteAsync();
        }

        public async Task<PhotoUser> GePhotoUserById(Guid Id)
        {
            return await _context.PhotoUsers.FindAsync(Id);
        }

        public async Task<PhotoUser> GetPhotoUserByTelegramId(long? telegramId)
        {
            return await _context.PhotoUsers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.User.TelegramId == telegramId);
        }

        public async Task<List<PhotoUser>> GetPhotoUsers()
        {
            return await _context.PhotoUsers.ToListAsync();
        }

        public async Task Update(PhotoUser photoUser)
        {
            _context.PhotoUsers.Update(photoUser);
            await _context.SaveChangesAsync();
        }
    }
}