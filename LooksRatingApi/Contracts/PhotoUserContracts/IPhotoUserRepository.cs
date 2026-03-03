using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoUserRepository
    {
        Task Create(PhotoUser photoUser);
        Task Delete(Guid Id);
        Task Update(PhotoUser photoUser);
        Task<PhotoUser> GePhotoUserById(Guid Id);
        Task<PhotoUser> GetPhotoUserByTelegramId(long? telegramId);
        Task<List<PhotoUser>> GetPhotoUsers();
    }
}