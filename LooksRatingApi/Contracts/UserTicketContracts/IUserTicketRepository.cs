using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.UserTicketContracts
{
    public interface IUserTicketRepository
    {
        Task Create(UserTicket ticket);
        Task Delete(Guid Id);
        Task Update(UserTicket ticket);
        Task<UserTicket?> GetTicketById(Guid Id);
        Task<UserTicket?> GetTicketByTelegramId(long telegramId);
        Task<List<UserTicket>> GetTicketsByUsersCity(string city);
        Task<bool> ExistsByReporterAndProfile(Guid reporterUserId, Guid photoProfileId);
    }
}