using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosId;

namespace LooksRatingApi.Tests.Unit.CQRS.PhotoUsers;

public sealed class GetTheBestWeekPhotosIdHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsIdsWithoutMutatingUserCounters()
    {
        var topStatsService = Substitute.For<ITheBestWeekTopStatsService>();
        topStatsService
            .GetCurrentWeekTopTelegramIdsAsync(Arg.Any<CancellationToken>())
            .Returns([1001L, 1002L]);

        var handler = new GetTheBestWeekPhotosIdHandler(topStatsService);

        var result = await handler.Handle(new GetTheBestWeekPhotosIdQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo([1001L, 1002L]);
        await topStatsService.Received(1).GetCurrentWeekTopTelegramIdsAsync(Arg.Any<CancellationToken>());
    }
}
