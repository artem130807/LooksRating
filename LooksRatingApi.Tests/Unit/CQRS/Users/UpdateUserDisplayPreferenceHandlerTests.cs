using FluentAssertions;
using LooksRatingApi.CQRS.Users.Command.UpdateUserDisplayPreference;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.Users.Command.RegisterUser;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.CQRS.Users;

public sealed class UpdateUserDisplayPreferenceHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ISeasonRepository _seasonRepository = Substitute.For<ISeasonRepository>();
    private readonly IPhotoProfileRepository _photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
    private readonly IPhotoRatingCacheService _photoRatingCacheService = Substitute.For<IPhotoRatingCacheService>();
    private readonly UpdateUserDisplayPreferenceHandler _handler;

    public UpdateUserDisplayPreferenceHandlerTests()
    {
        _handler = new UpdateUserDisplayPreferenceHandler(
            _userRepository,
            _seasonRepository,
            _photoProfileRepository,
            _photoRatingCacheService);
    }

    [Fact]
    public async Task Handle_ShowTelegramUsername_ClearsCustomName()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 88001,
            TelegramUsername = "old_user",
            Name = "Артём",
        };
        _userRepository.GetUserByTelegramId(88001).Returns(user);
        _seasonRepository.GetCurrent().Returns((Season?)null);

        var result = await _handler.Handle(
            new UpdateUserDisplayPreferenceCommand(88001, "new_user", true, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Name.Should().BeNull();
        user.TelegramUsername.Should().Be("new_user");
        result.Value.DisplayName.Should().Be("@new_user");
        result.Value.UsesTelegramUsernameAsDisplay.Should().BeTrue();
        await _userRepository.Received(1).Update(user);
        await _photoRatingCacheService.DidNotReceiveWithAnyArgs()
            .SyncProfileDisplayNameAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_HideTelegramUsername_SetsCustomName()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 88002,
            TelegramUsername = "rated_user",
            Name = null,
        };
        _userRepository.GetUserByTelegramId(88002).Returns(user);
        _seasonRepository.GetCurrent().Returns((Season?)null);

        var result = await _handler.Handle(
            new UpdateUserDisplayPreferenceCommand(88002, "rated_user", false, "Мария"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Name.Should().Be("Мария");
        result.Value.DisplayName.Should().Be("Мария");
        result.Value.UsesTelegramUsernameAsDisplay.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShowTelegramUsername_WithoutUsername_ReturnsError()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 88003,
            TelegramUsername = null,
            Name = "Скрытый",
        };
        _userRepository.GetUserByTelegramId(88003).Returns(user);

        var result = await _handler.Handle(
            new UpdateUserDisplayPreferenceCommand(88003, null, true, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RegisterUserErrors.TelegramUsernameRequiredForDisplay);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsError()
    {
        _userRepository.GetUserByTelegramId(88004).Returns((User?)null);

        var result = await _handler.Handle(
            new UpdateUserDisplayPreferenceCommand(88004, "user", false, "Имя"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UpdateUserDisplayPreferenceErrors.UserNotFound);
    }

    [Fact]
    public async Task Handle_UpdatesRedisDisplayNameForCurrentSeasonProfiles()
    {
        var seasonId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 88005,
            TelegramUsername = "rated_user",
            Name = null,
        };
        _userRepository.GetUserByTelegramId(88005).Returns(user);
        _seasonRepository.GetCurrent().Returns(new Season { Id = seasonId });
        _photoProfileRepository
            .GetByTelegramAndSeasonListAsync(88005, seasonId, Arg.Any<CancellationToken>())
            .Returns(new List<PhotoProfile>
            {
                new() { Id = profileId, SeasonId = seasonId, UserId = user.Id },
            });

        var result = await _handler.Handle(
            new UpdateUserDisplayPreferenceCommand(88005, "rated_user", false, "Никита"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _photoRatingCacheService.Received(1).SyncProfileDisplayNameAsync(
            profileId,
            "Никита",
            Arg.Any<CancellationToken>());
    }
}
