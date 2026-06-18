using FluentAssertions;
using LooksRatingApi.Models;
using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class UserPublicDisplayNameTests
{
    [Fact]
    public void UsesTelegramUsernameAsDisplay_IsTrueWhenNameIsEmpty()
    {
        var user = new User
        {
            TelegramUsername = "rated_user",
            Name = null,
        };

        UserPublicDisplayName.UsesTelegramUsernameAsDisplay(user).Should().BeTrue();
        UserPublicDisplayName.Resolve(user).Should().Be("@rated_user");
    }

    [Fact]
    public void UsesTelegramUsernameAsDisplay_IsFalseWhenCustomNameSet()
    {
        var user = new User
        {
            TelegramUsername = "rated_user",
            Name = "Мария",
        };

        UserPublicDisplayName.UsesTelegramUsernameAsDisplay(user).Should().BeFalse();
        UserPublicDisplayName.Resolve(user).Should().Be("Мария");
    }
}
