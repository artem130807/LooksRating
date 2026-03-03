using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.UserSessionContracts
{
    public interface IUserSessionRepository
    {
        Task<UserSession?> GetByTelegramId(long telegramId);
        Task Create(UserSession session);
        Task Update(UserSession session);
    }
}