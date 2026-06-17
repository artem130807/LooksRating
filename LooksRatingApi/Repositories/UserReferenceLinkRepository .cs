using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class UserReferenceLinkRepository : IUserReferenceLinkRepository
    {
        private readonly LooksRatingDbContext _context;
        public UserReferenceLinkRepository(LooksRatingDbContext context)
        {
            _context = context;
        }
        public async Task Add(UserReferenceLink userReferenceLink)
        {
            _context.UserReferenceLinks.Add(userReferenceLink);
        }

        public async Task<UserReferenceLink> GetByUserId(Guid userId)
        {
            return await _context.UserReferenceLinks.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}