using FluentAssertions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WritingOffSparksEntity = LooksRatingApi.Models.WritingOffSparks;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class GetWritingOffSparksOrchestratorTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsItem_WhenEntityExists()
    {
        await using var context = CreateContext();
        var seeded = await SeedWritingOffSparksAsync(context, "moscow", 100_001, 1200m, 100);
        var orchestrator = CreateOrchestrator(context);

        var result = await orchestrator.GetByIdAsync(seeded.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.Item.Should().NotBeNull();
        result.Value.Item.Id.Should().Be(seeded.Id.ToString());
        result.Value.Item.UserId.Should().Be(seeded.UserId.ToString());
        result.Value.Item.TelegramId.Should().Be(100_001);
        result.Value.Item.City.Should().Be("moscow");
        result.Value.Item.SparksCount.Should().Be(1200);
        result.Value.Item.Stars.Should().Be(100);
        result.Value.Item.Status.Should().Be(LooksRatingGrpc.OutputStatusEnum.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenEntityMissing()
    {
        await using var context = CreateContext();
        var orchestrator = CreateOrchestrator(context);

        var result = await orchestrator.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Списание искр не найдено");
        result.Value.Item.Should().BeNull();
    }

    private static GetWritingOffSparksOrchestrator CreateOrchestrator(LooksRatingDbContext context) =>
        new(new WritingOffSparksRepository(context));

    private static async Task<WritingOffSparksEntity> SeedWritingOffSparksAsync(
        LooksRatingDbContext context,
        string city,
        long telegramId,
        decimal sparksCount,
        int stars)
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
            sparksCount,
            $"test-key-{telegramId}-{stars}",
            stars,
            city).Value;

        context.Users.Add(user);
        context.WritingOffSparks.Add(writingOffSparks);
        await context.SaveChangesAsync();

        return writingOffSparks;
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
