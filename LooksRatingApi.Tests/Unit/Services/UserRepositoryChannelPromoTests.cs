using LooksRatingApi;
using LooksRatingApi.Enums;
using LooksRatingApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class UserRepositoryChannelPromoTests
{
    [Fact]
    public async Task GetTelegramIdsPagedAsync_WhenOnlyUnsubscribed_ReturnsEligibleUsers()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            CreateUser(1001, subscribed: false),
            CreateUser(1002, subscribed: true),
            CreateUser(1003, subscribed: false));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);
        var page = await repository.GetTelegramIdsPagedAsync(
            page: 1,
            pageSize: 10,
            onlyUnsubscribedChannel: true,
            cancellationToken: CancellationToken.None);

        page.Data.Should().BeEquivalentTo([1001L, 1003L]);
        page.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetTelegramIdsPagedAsync_WhenFilterDisabled_ReturnsAllUsers()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            CreateUser(2001, subscribed: false),
            CreateUser(2002, subscribed: true));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);
        var page = await repository.GetTelegramIdsPagedAsync(
            page: 1,
            pageSize: 10,
            onlyUnsubscribedChannel: false,
            cancellationToken: CancellationToken.None);

        page.Data.Should().BeEquivalentTo([2001L, 2002L]);
        page.Count.Should().Be(2);
    }

    private static LooksRatingApi.Models.User CreateUser(long telegramId, bool subscribed) =>
        new()
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = VipStatus.Unavaillable,
            IssubscribeChannel = subscribed,
        };

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
