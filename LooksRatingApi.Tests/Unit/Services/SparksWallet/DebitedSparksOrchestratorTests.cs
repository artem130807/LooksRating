using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class DebitedSparksOrchestratorTests
{
    [Theory]
    [InlineData(50)]
    [InlineData(150)]
    [InlineData(500)]
    public async Task DebitedSparks_RejectsUnknownStarTier(int starsCount)
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91001);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 5000m).Value);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.DebitedSparks(91001, starsCount, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Недопустимая стоимость подарка");
    }

    [Fact]
    public async Task DebitedSparks_RejectsInsufficientBalanceAtTwelveToOneRate()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91002);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 1199m).Value);
        await context.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(context, user);

        var result = await orchestrator.DebitedSparks(91002, 100, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Недостаточно искр на балансе");
    }

    [Fact]
    public async Task DebitedSparks_DebitsTwelveSparksPerStarOnSuccess()
    {
        await using var context = CreateContext();
        var user = CreateVipUser(91003);
        context.Users.Add(user);
        await context.SparksLedgers.AddAsync(LooksRatingApi.Models.SparksWallet.Create(user.Id, 1200m).Value);
        await context.SaveChangesAsync();

        var debitService = Substitute.For<ICurrencyDebitedService>();
        debitService
            .Debited(user.Id, 1200m, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var orchestrator = CreateOrchestrator(context, user, debitService);

        var result = await orchestrator.DebitedSparks(91003, 100, CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        await debitService.Received(1).Debited(user.Id, 1200m, Arg.Any<CancellationToken>());
    }

    private static User CreateVipUser(long telegramId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = VipStatus.Availlable,
        };

    private static DebitedSparksOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User user,
        ICurrencyDebitedService? debitService = null)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .GetUserByTelegramId(user.TelegramId)
            .Returns(_ => context.Users.First(u => u.TelegramId == user.TelegramId));

        debitService ??= Substitute.For<ICurrencyDebitedService>();
        debitService
            .Debited(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new DebitedSparksOrchestrator(
            debitService,
            NullLogger<DebitedSparksOrchestrator>.Instance,
            context,
            userRepository,
            new SparksLedgerRepository(context));
    }

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
