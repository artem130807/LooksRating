using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Cqrs.Users.Command.RegisterUser;
using LooksRatingApi.Enums;
using LooksRatingApi.Repositories;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Integration.Cqrs;

[Collection(IntegrationCollection.Name)]
public sealed class RegisterUserCommandHandlerTests
{
    private readonly PostgresFixture _postgres;

    public RegisterUserCommandHandlerTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [SkippableFact]
    public async Task Handle_CreatesUserSparksWalletAndLinksSession()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);
        await TestDataBuilder.SeedSessionAsync(context, 7001, BotSessionState.AwaitingDisplayName);

        var validator = Substitute.For<IUserRegisterValidator>();
        validator.ValidateAsync(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        var handler = new RegisterUserCommandHandler(
            new UserRepository(context),
            validator,
            new UserSessionRepository(context),
            new SparksLedgerRepository(context),
            context,
            NullLogger<RegisterUserCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterUserCommand(7001, "test_user", true, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await context.Users.SingleAsync(u => u.TelegramId == 7001);
        user.TelegramUsername.Should().Be("test_user");
        user.Name.Should().BeNull();

        var sparksBalance = await context.SparksLedgers
            .Where(wallet => wallet.UserId == user.Id)
            .SumAsync(wallet => wallet.SparksCount);
        sparksBalance.Should().Be(10m);

        var session = await context.UserSessions.SingleAsync(s => s.TelegramId == 7001);
        session.UserId.Should().Be(user.Id);
        session.State.Should().Be(BotSessionState.Registered.ToString());
    }

    [SkippableFact]
    public async Task Handle_WhenValidationFails_DoesNotPersistUser()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var validator = Substitute.For<IUserRegisterValidator>();
        validator.ValidateAsync(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>("invalid"));

        var handler = new RegisterUserCommandHandler(
            new UserRepository(context),
            validator,
            new UserSessionRepository(context),
            new SparksLedgerRepository(context),
            context,
            NullLogger<RegisterUserCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterUserCommand(7002, "broken", true, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        (await context.Users.CountAsync()).Should().Be(0);
    }
}
