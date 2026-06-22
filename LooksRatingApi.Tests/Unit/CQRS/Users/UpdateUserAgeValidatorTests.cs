using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.Users.Command.UpdateUserAge;
using LooksRatingApi.Models;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.CQRS.Users;

public sealed class UpdateUserAgeValidatorTests
{
    [Theory]
    [InlineData(14)]
    [InlineData(46)]
    [InlineData(0)]
    public async Task ValidateAsync_WhenAgeIsSupported_ReturnsSuccess(int age)
    {
        var user = CreateUser();
        var validator = CreateValidator(user);

        var result = await validator.ValidateAsync(
            new UpdateUserAgeCommand(user.TelegramId, age),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(11)]
    [InlineData(67)]
    [InlineData(100)]
    public async Task ValidateAsync_WhenAgeIsUnsupported_ReturnsInvalidAge(int age)
    {
        var user = CreateUser();
        var validator = CreateValidator(user);

        var result = await validator.ValidateAsync(
            new UpdateUserAgeCommand(user.TelegramId, age),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UpdateUserAgeErrors.InvalidAge);
    }

    private static UpdateUserAgeValidator CreateValidator(User user)
    {
        var repository = Substitute.For<IUserRepository>();
        repository.GetUserByTelegramId(user.TelegramId).Returns(user);
        return new UpdateUserAgeValidator(repository);
    }

    private static User CreateUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            TelegramId = 7001,
        };
}
