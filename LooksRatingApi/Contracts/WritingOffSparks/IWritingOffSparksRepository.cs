using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Filters;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Contracts.WritingOffSparks
{
    public interface IWritingOffSparksRepository
    {
        Task Add(Models.WritingOffSparks writingOffSparks);
        Task<Models.WritingOffSparks?> GetById(Guid id);
        Task<Models.WritingOffSparks?> GetByUserIdAndIdempotencyKey(Guid userId, string idempotencyKey);
        Task<PagedResult<Models.WritingOffSparks>> GetPendingWritingsOffSparks(PageParams pageParams, string city);
        Task<List<string>> GetCitiesWithPendingWritingsOffSparks();
        Task SaveChanges();
    }
}