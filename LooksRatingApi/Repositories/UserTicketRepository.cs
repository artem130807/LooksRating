using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class UserTicketRepository:IUserTicketRepository
    {
        private readonly LooksRatingDbContext _context;
        public UserTicketRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task Create(UserTicket ticket)
        {
            _context.UserTickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid Id)
        {
            await _context.UserTickets.Where(x => x.Id == Id).ExecuteDeleteAsync();
        }

        public async Task<UserTicket> GetTicketById(Guid Id)
        {
            return await _context.UserTickets.FindAsync(Id);
        }

        public async Task<UserTicket> GetTicketByTelegramId(long? telegramId)
        {
            return await _context.UserTickets.Include(x => x.User).FirstOrDefaultAsync(x => x.User.TelegramId == telegramId);
        }

        public async Task<List<UserTicket>> GetTicketsByUsersCity(string city)
        {
            return await _context.UserTickets
                .Include(x => x.User)
                .Where(x => x.User.City.Value == city)
                .ToListAsync();
        }

        public async Task Update(UserTicket ticket)
        {
            _context.UserTickets.Update(ticket);
            await _context.SaveChangesAsync();
        }
    }
}