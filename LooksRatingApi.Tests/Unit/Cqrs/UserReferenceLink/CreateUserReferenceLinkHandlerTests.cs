using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.UserReferenceLink.Command.CreateUserReferenceLink;
using LooksRatingApi.Models;

namespace LooksRatingApi.Tests.Unit.Cqrs.UserReferenceLink;

public sealed class CreateUserReferenceLinkHandlerTests
{
    [Fact]
    public async Task Handle_WhenLinkAlreadyExists_ReturnsExistingLink()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 9201,
            Name = "User 9201",
        };
        var existing = LooksRatingApi.Models.UserReferenceLink.Create(user.Id).Value;

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(9201).Returns(user);

        var linkRepository = Substitute.For<IUserReferenceLinkRepository>();
        linkRepository
            .EnsureLinkExistsAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new CreateUserReferenceLinkHandler(userRepository, linkRepository);

        var first = await handler.Handle(new CreateUserReferenceLinkCommand(9201), CancellationToken.None);
        var second = await handler.Handle(new CreateUserReferenceLinkCommand(9201), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value.Should().Be(existing.Link);
        second.Value.Should().Be(existing.Link);
        await linkRepository.Received(2).EnsureLinkExistsAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(9202).Returns((User?)null);

        var handler = new CreateUserReferenceLinkHandler(
            userRepository,
            Substitute.For<IUserReferenceLinkRepository>());

        var result = await handler.Handle(new CreateUserReferenceLinkCommand(9202), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
