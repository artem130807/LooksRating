using FluentAssertions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using WritingOffSparksEntity = LooksRatingApi.Models.WritingOffSparks;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class GetWritingsOffSparksOrchestratorTests
{
    [Fact]
    public async Task GetByCityAsync_ReturnsOnlyItemsForRequestedCity()
    {
        await using var context = CreateContext();
        await SeedAsync(context, "moscow", 100_001);
        await SeedAsync(context, "moscow", 100_002);
        await SeedAsync(context, "spb", 100_003);

        var orchestrator = CreateOrchestrator(context, normalize: city => city.Trim().ToLowerInvariant());

        var result = await orchestrator.GetByCityAsync("Moscow", page: 1, pageSize: 10, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(x => x.City == "moscow");
    }

    [Fact]
    public async Task GetByCityAsync_PaginatesResults()
    {
        await using var context = CreateContext();
        for (var i = 0; i < 3; i++)
        {
            await SeedAsync(context, "kazan", 200_000 + i);
        }

        var orchestrator = CreateOrchestrator(context, normalize: city => city);

        var result = await orchestrator.GetByCityAsync("kazan", page: 1, pageSize: 2, CancellationToken.None);

        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetByCityAsync_ReturnsEmptyList_WhenCityHasNoItems()
    {
        await using var context = CreateContext();
        await SeedAsync(context, "moscow", 100_004);
        var orchestrator = CreateOrchestrator(context, normalize: city => city);

        var result = await orchestrator.GetByCityAsync("novosibirsk", page: 1, pageSize: 10, CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetByCityAsync_RejectsEmptyCity()
    {
        await using var context = CreateContext();
        var orchestrator = CreateOrchestrator(context, normalize: city => city);

        var result = await orchestrator.GetByCityAsync("  ", page: 1, pageSize: 10, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Город не указан");
    }

    [Fact]
    public async Task GetByCityAsync_RejectsCity_WhenNormalizationReturnsEmpty()
    {
        await using var context = CreateContext();
        var orchestrator = CreateOrchestrator(context, normalize: _ => string.Empty);

        var result = await orchestrator.GetByCityAsync("!!!", page: 1, pageSize: 10, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Город не указан");
    }

    private static GetWritingsOffSparksOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        Func<string, string> normalize)
    {
        var normalizeCityNameService = Substitute.For<INormalizeCityNameService>();
        normalizeCityNameService.Normalize(Arg.Any<string>()).Returns(call => normalize(call.Arg<string>()));

        return new GetWritingsOffSparksOrchestrator(
            new WritingOffSparksRepository(context),
            normalizeCityNameService);
    }

    [Fact]
    public async Task GetByCityAsync_ExcludesNonPendingRequests()
    {
        await using var context = CreateContext();
        var pending = await SeedAsync(context, "moscow", 100_010, OutputStatusEnum.Pending);
        await SeedAsync(context, "moscow", 100_011, OutputStatusEnum.Confirmed);

        var orchestrator = CreateOrchestrator(context, normalize: city => city);

        var result = await orchestrator.GetByCityAsync("moscow", page: 1, pageSize: 10, CancellationToken.None);

        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle().Which.Id.Should().Be(pending.Id.ToString());
    }

    private static async Task<WritingOffSparksEntity> SeedAsync(
        LooksRatingDbContext context,
        string city,
        long telegramId,
        OutputStatusEnum status = OutputStatusEnum.Pending)
    {
        var user = new Models.User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
        };

        var writingOffSparks = WritingOffSparksEntity.Create(
            user.Id,
            1200m,
            $"test-key-{telegramId}-{city}",
            100,
            city).Value;
        if (status != OutputStatusEnum.Pending)
        {
            writingOffSparks.UpdateStatus(status);
        }

        context.Users.Add(user);
        context.WritingOffSparks.Add(writingOffSparks);
        await context.SaveChangesAsync();

        return writingOffSparks;
    }

    private static async Task<WritingOffSparksEntity> SeedAsync(
        LooksRatingDbContext context,
        string city,
        long telegramId)
        => await SeedAsync(context, city, telegramId, OutputStatusEnum.Pending);

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
