using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.Users.Query.GetUserStats;
using LooksRatingApi.Models;

namespace LooksRatingApi.Tests.Unit.CQRS.Users;

public sealed class GetUserStatsHandlerTests
{
    [Fact]
    public async Task Handle_ComputesStatsOnReadWithoutMutatingStoredCounters()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42_001,
            CountInTop = 999,
        };

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(42_001).Returns(user);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository
            .CountSeasonsWithProfileAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(2);

        var topStatsService = Substitute.For<ITheBestWeekTopStatsService>();
        topStatsService
            .CountWeekAppearancesForTelegramIdAsync(42_001, Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = new GetUserStatsHandler(
            userRepository,
            photoProfileRepository,
            topStatsService);

        var result = await handler.Handle(new GetUserStatsQuery(42_001), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CountInTop.Should().Be(1);
        result.Value.SeasonsWithPhoto.Should().Be(2);
        await userRepository.DidNotReceiveWithAnyArgs().AddCountInTop(default!);
        await photoProfileRepository.Received(1).CountSeasonsWithProfileAsync(user.Id, Arg.Any<CancellationToken>());
        await topStatsService.Received(1).CountWeekAppearancesForTelegramIdAsync(42_001, Arg.Any<CancellationToken>());
    }
}
