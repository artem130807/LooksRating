using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts
{
    public interface IUserReferenceLinkRepository
    {
        Task Add(UserReferenceLink userReferenceLink);
        Task<UserReferenceLink> GetByUserId(Guid userId);
        Task SaveChangesAsync();
    }
}