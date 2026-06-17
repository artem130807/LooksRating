using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.CQRS.Reviews.Query.GetMilestoneReviewers;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using LooksRatingApi.Models;

namespace LooksRatingApi.Tests.Unit.CQRS.Reviews;

public sealed class GetMilestoneReviewersHandlerTests
{
    private readonly IReviewMilestoneNotificationRepository _notificationRepository =
        Substitute.For<IReviewMilestoneNotificationRepository>();
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>();
    private readonly IPhotoProfileRepository _photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
    private readonly GetMilestoneReviewersHandler _handler;

    public GetMilestoneReviewersHandlerTests()
    {
        _handler = new GetMilestoneReviewersHandler(
            _notificationRepository,
            _reviewRepository,
            _photoProfileRepository);
    }

    [Fact]
    public async Task Handle_UsesCycleNumberToFetchReviewers()
    {
        var profileId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var notification = ReviewMilestoneNotification.CreatePending(profileId, 42, cycleNumber: 2);

        _notificationRepository
            .GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        _photoProfileRepository
            .GetByIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns(new PhotoProfile
            {
                Id = profileId,
                SeasonId = seasonId,
                UserId = Guid.NewGuid(),
                Rating = 8m,
                RatingCount = 20,
                Rank = RankEnum.Cute,
                Status = StatusEnum.Active,
                CityNomination = CityVo.Create("moscow").Value,
                AgeNomination = 22,
                GenderNomination = GenderEnum.Female,
                CreatedAt = DateTime.UtcNow,
            });

        _reviewRepository
            .GetReviewersForProfileCycleAsync(
                profileId,
                2,
                ReviewSequenceConstants.MaxReviewsCount,
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Review>());

        var result = await _handler.Handle(
            new GetMilestoneReviewersQuery(notification.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _reviewRepository.Received(1).GetReviewersForProfileCycleAsync(
            profileId,
            2,
            ReviewSequenceConstants.MaxReviewsCount,
            Arg.Any<CancellationToken>());
    }
}
