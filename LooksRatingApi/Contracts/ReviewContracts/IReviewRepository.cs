using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.ReviewContracts
{
    public interface IReviewRepository
    {
        Task Create(Review review);
        Task Delete(Guid Id);
        Task Update(Review review);
        Task<Review> GetReviewById(Guid Id);
        Task<List<Review>> GetReviewsByTelegramId(long? telegramId);
    }
}