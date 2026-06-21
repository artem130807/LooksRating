using FluentAssertions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.Orchestrators;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class CreateWritingOffSparksOrchestratorTests
{
    private const string IdempotencyKey = "writing-off-sparks:93001:callback-1";

    [Fact]
    public async Task ConfirmedWriting_PersistsEntity_WhenDataIsValid()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_001);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        await SeedActiveDebitAsync(context, user.Id);

        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var result = await orchestrator.ConfirmedWriting(
            93_001,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();

        var entity = await context.WritingOffSparks.SingleAsync();
        entity.UserId.Should().Be(user.Id);
        entity.City.Should().Be("moscow");
        entity.SparksCount.Should().Be(1200m);
        entity.Stars.Should().Be(100);
        entity.IdempotencyKey.Should().Be(IdempotencyKey);
    }

    [Fact]
    public async Task ConfirmedWriting_ReturnsSuccessWithoutDuplicate_WhenKeyAlreadyExists()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_004);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        await SeedActiveDebitAsync(context, user.Id);
        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var first = await orchestrator.ConfirmedWriting(
            93_004,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);
        var second = await orchestrator.ConfirmedWriting(
            93_004,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        first.Value.Success.Should().BeTrue();
        second.Value.Success.Should().BeTrue();
        second.Value.Message.Should().Be("Заявка уже создана");
        (await context.WritingOffSparks.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ConfirmedWriting_ReturnsFailure_WhenKeyMissing()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_005);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var result = await orchestrator.ConfirmedWriting(93_005, 1200m, "  ", 100, CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Ключ идемпотентности не указан");
        (await context.WritingOffSparks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmedWriting_ReturnsFailure_WhenExchangeAmountsMismatch()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_003);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        await SeedActiveDebitAsync(context, user.Id);
        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var result = await orchestrator.ConfirmedWriting(
            93_003,
            999m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Недопустимая стоимость обмена");
        (await context.WritingOffSparks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmedWriting_ReturnsFailure_WhenActiveDebitMissing()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_007);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var result = await orchestrator.ConfirmedWriting(
            93_007,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Списание искр не найдено");
        (await context.WritingOffSparks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmedWriting_ReturnsFailure_WhenDebitIsCompensated()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_008);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        var debit = SparksDebitIdempotency.Create(user.Id, IdempotencyKey, Guid.NewGuid(), 1200m, 100).Value;
        debit.MarkCompensated();
        context.SparksDebitIdempotency.Add(debit);
        await context.SaveChangesAsync();
        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var result = await orchestrator.ConfirmedWriting(
            93_008,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Списание искр не найдено");
        (await context.WritingOffSparks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmedWriting_ReactivatesCancelledRequest_WithSameKey()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_006);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        await SeedActiveDebitAsync(context, user.Id);
        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var cancelled = Models.WritingOffSparks.Create(user.Id, 1200m, IdempotencyKey, 100, "moscow").Value;
        cancelled.UpdateStatus(Enums.OutputStatusEnum.Cancelled);
        context.WritingOffSparks.Add(cancelled);
        await context.SaveChangesAsync();

        var result = await orchestrator.ConfirmedWriting(
            93_006,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        result.Value.Success.Should().BeTrue();
        (await context.WritingOffSparks.CountAsync()).Should().Be(1);

        var reloaded = await context.WritingOffSparks.SingleAsync();
        reloaded.Id.Should().Be(cancelled.Id);
        reloaded.Status.Should().Be(Enums.OutputStatusEnum.Pending);
    }

    [Fact]
    public async Task ConfirmedWriting_ReturnsFailure_WhenReactivatingWithoutActiveDebit()
    {
        await using var context = CreateContext();
        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var user = await TestDataBuilder.SeedUserAsync(context, 93_009);
        var profile = await TestDataBuilder.SeedPhotoProfileAsync(context, user, season);
        var orchestrator = CreateOrchestrator(context, user, season, profile);

        var cancelled = Models.WritingOffSparks.Create(user.Id, 1200m, IdempotencyKey, 100, "moscow").Value;
        cancelled.UpdateStatus(Enums.OutputStatusEnum.Cancelled);
        context.WritingOffSparks.Add(cancelled);
        await context.SaveChangesAsync();

        var result = await orchestrator.ConfirmedWriting(
            93_009,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Списание искр не найдено");

        var reloaded = await context.WritingOffSparks.SingleAsync();
        reloaded.Status.Should().Be(Enums.OutputStatusEnum.Cancelled);
    }

    [Fact]
    public async Task ConfirmedWriting_ReturnsFailure_WhenUserMissing()
    {
        await using var context = CreateContext();
        var orchestrator = CreateOrchestrator(context, user: null, season: null, profile: null);

        var result = await orchestrator.ConfirmedWriting(
            93_002,
            1200m,
            IdempotencyKey,
            100,
            CancellationToken.None);

        result.Value.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Пользователь не найден");
        (await context.WritingOffSparks.CountAsync()).Should().Be(0);
    }

    private static async Task SeedActiveDebitAsync(
        LooksRatingDbContext context,
        Guid userId,
        string idempotencyKey = IdempotencyKey,
        decimal sparks = 1200m,
        int stars = 100)
    {
        var debit = SparksDebitIdempotency.Create(userId, idempotencyKey, Guid.NewGuid(), sparks, stars).Value;
        context.SparksDebitIdempotency.Add(debit);
        await context.SaveChangesAsync();
    }

    private static CreateWritingOffSparksOrchestrator CreateOrchestrator(
        LooksRatingDbContext context,
        User? user,
        Season? season = null,
        PhotoProfile? profile = null)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .GetUserByTelegramId(Arg.Any<long>())
            .Returns(call => call.Arg<long>() == user?.TelegramId ? user : null);

        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        if (user is not null && season is not null && profile is not null)
        {
            photoProfileRepository
                .GetByUserAndSeasonAsync(user.Id, season.Id, Arg.Any<CancellationToken>())
                .Returns(profile);
        }

        return new CreateWritingOffSparksOrchestrator(
            new WritingOffSparksRepository(context),
            userRepository,
            context,
            NullLogger<CreateWritingOffSparksOrchestrator>.Instance,
            photoProfileRepository,
            seasonRepository,
            new SparksDebitIdempotencyRepository(context));
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
