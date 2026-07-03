using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LooksRatingApi.Filters;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.UserContracts
{
    public interface IUserRepository
    {
        Task Create(User user);
        Task Delete(Guid Id);
        Task Update(User user);
        Task<User?> GetUserById(Guid Id);
        Task<User?> GetUserByTelegramId(long TelegramId);
        Task AddCountInTop(List<long> ids);
        // Task<int> CountTimesInTopAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<User>> GetUsers();
        Task<PagedResult<User>> GetUsersToPagedAsync(PageParams pageParams);
        Task<PagedResult<long>> GetTelegramIdsPagedAsync(
            int page,
            int pageSize,
            bool onlyUnsubscribedChannel = false,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync();
    }
}