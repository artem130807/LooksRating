using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.UserReferenceLink;
using LooksRatingApi.CQRS.UserReferenceLink.Query.GetUserReferenceLink;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.Tests.Unit.Cqrs.UserReferenceLink;

public sealed class GetUserReferenceLinkHandlerTests
{
    [Fact]
    public async Task Handle_WhenLinkExists_ReturnsLinkAndInviteStats()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 9210,
            Name = "User 9210",
        };
        var existing = LooksRatingApi.Models.UserReferenceLink.Create(user.Id).Value;
        existing.AddCountInvited();
        existing.AddCountInvited();

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(9210).Returns(user);

        var linkRepository = Substitute.For<IUserReferenceLinkRepository>();
        linkRepository
            .GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new GetUserReferenceLinkHandler(userRepository, linkRepository);
        var result = await handler.Handle(new GetUserReferenceLinkQuery(9210), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Link.Should().Be(existing.Link);
        result.Value.CountInvited.Should().Be(2);
        result.Value.MaxInvited.Should().Be(ReferralSparksRules.MaxInvitedUsers);
    }

    [Fact]
    public async Task Handle_WhenLinkMissing_ReturnsFailure()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 9211,
            Name = "User 9211",
        };

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(9211).Returns(user);

        var linkRepository = Substitute.For<IUserReferenceLinkRepository>();
        linkRepository
            .GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((LooksRatingApi.Models.UserReferenceLink?)null);

        var handler = new GetUserReferenceLinkHandler(userRepository, linkRepository);
        var result = await handler.Handle(new GetUserReferenceLinkQuery(9211), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
