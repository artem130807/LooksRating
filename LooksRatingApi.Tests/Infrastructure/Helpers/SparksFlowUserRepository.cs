using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Filters;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Tests.Infrastructure.Helpers;

/// <summary>
/// Avoids eager-loading <see cref="User.RecomendationSettings"/> for in-memory integration tests.
/// </summary>
internal sealed class SparksFlowUserRepository : IUserRepository
{
    private readonly LooksRatingDbContext _context;
    private readonly UserRepository _inner;

    public SparksFlowUserRepository(LooksRatingDbContext context)
    {
        _context = context;
        _inner = new UserRepository(context);
    }

    public Task<User?> GetUserByTelegramId(long telegramId) =>
        _context.Users.FirstOrDefaultAsync(user => user.TelegramId == telegramId);

    public Task<User?> GetUserById(Guid id) => _inner.GetUserById(id);

    public Task Create(User user) => _inner.Create(user);

    public Task Delete(Guid id) => _inner.Delete(id);

    public Task Update(User user) => _inner.Update(user);

    public Task AddCountInTop(List<long> ids) => _inner.AddCountInTop(ids);

    public Task<List<User>> GetUsers() => _inner.GetUsers();

    public Task<PagedResult<long>> GetTelegramIdsPagedAsync(
        int page,
        int pageSize,
        bool onlyUnsubscribedChannel = false,
        CancellationToken cancellationToken = default) =>
        _inner.GetTelegramIdsPagedAsync(page, pageSize, onlyUnsubscribedChannel, cancellationToken);

    public Task SaveChangesAsync() => _inner.SaveChangesAsync();
}
