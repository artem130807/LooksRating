using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.UserContracts
{
    public interface IUserRepository
    {
        Task Create(User user);
        Task Delete(Guid Id);
        Task Update(User user);
        Task<User> GetUserById(Guid Id);
        Task<User> GetUserByTelegramId(long TelegramId);
        Task<List<User>> GetUsers();
    }
}