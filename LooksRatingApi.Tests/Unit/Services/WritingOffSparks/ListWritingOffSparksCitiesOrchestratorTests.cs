using FluentAssertions;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WritingOffSparksEntity = LooksRatingApi.Models.WritingOffSparks;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class ListWritingOffSparksCitiesOrchestratorTests
{
    [Fact]
    public async Task ListCitiesAsync_ReturnsOnlyCitiesWithPendingRequests()
    {
        await using var context = CreateContext();
        await SeedAsync(context, "moscow", 100_001, OutputStatusEnum.Pending);
        await SeedAsync(context, "moscow", 100_002, OutputStatusEnum.Confirmed);
        await SeedAsync(context, "kazan", 100_003, OutputStatusEnum.Pending);

        var orchestrator = new ListWritingOffSparksCitiesOrchestrator(new WritingOffSparksRepository(context));

        var result = await orchestrator.ListCitiesAsync(CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        result.Value.Cities.Should().BeEquivalentTo(["kazan", "moscow"]);
    }

    private static async Task SeedAsync(
        LooksRatingDbContext context,
        string city,
        long telegramId,
        OutputStatusEnum status)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
        };

        var writingOffSparks = WritingOffSparksEntity.Create(
            user.Id,
            1200m,
            $"test-key-{city}",
            100,
            city).Value;
        writingOffSparks.UpdateStatus(status);

        context.Users.Add(user);
        context.WritingOffSparks.Add(writingOffSparks);
        await context.SaveChangesAsync();
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
