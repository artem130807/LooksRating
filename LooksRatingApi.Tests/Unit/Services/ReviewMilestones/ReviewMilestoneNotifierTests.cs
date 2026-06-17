using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services.ReviewMilestones;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services.ReviewMilestones;

public sealed class ReviewMilestoneNotifierTests
{
    private readonly IPhotoProfileRepository _photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
    private readonly IReviewMilestoneNotificationRepository _notificationRepository =
        Substitute.For<IReviewMilestoneNotificationRepository>();
    private readonly ReviewMilestoneNotifier _notifier;

    public ReviewMilestoneNotifierTests()
    {
        _notifier = new ReviewMilestoneNotifier(
            _photoProfileRepository,
            _notificationRepository,
            NullLogger<ReviewMilestoneNotifier>.Instance);
    }

    [Fact]
    public async Task TryNotifyAsync_WhenCountIsTen_CreatesPendingNotification()
    {
        var profileId = Guid.NewGuid();
        var owner = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 9001,
            TelegramUsername = "owner",
            Status = VipStatus.Unavaillable,
        };
        var profile = new PhotoProfile
        {
            Id = profileId,
            UserId = owner.Id,
            User = owner,
            SeasonId = Guid.NewGuid(),
            Rating = 8m,
            RatingCount = 10,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = LooksRatingApi.Domain.Vo.CityVo.Create("moscow").Value,
            AgeNomination = 22,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };

        _photoProfileRepository.GetByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _notificationRepository
            .TryAddPendingAsync(Arg.Any<ReviewMilestoneNotification>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var reviewEvent = new CreateReviewEvent(Guid.NewGuid(), profileId, reviewsCount: 10, isNewReview: true);
        await _notifier.TryNotifyAsync(reviewEvent, CancellationToken.None);

        await _notificationRepository.Received(1).TryAddPendingAsync(
            Arg.Is<ReviewMilestoneNotification>(n =>
                n.PhotoProfileId == profileId
                && n.OwnerTelegramId == 9001
                && n.CycleNumber == 1
                && n.Status == ReviewMilestoneNotificationStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryNotifyAsync_WhenNotNewReview_DoesNothing()
    {
        var reviewEvent = new CreateReviewEvent(Guid.NewGuid(), Guid.NewGuid(), reviewsCount: 10, isNewReview: false);
        await _notifier.TryNotifyAsync(reviewEvent, CancellationToken.None);

        await _photoProfileRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _notificationRepository.DidNotReceive()
            .TryAddPendingAsync(Arg.Any<ReviewMilestoneNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryNotifyAsync_WhenCountIsNine_DoesNothing()
    {
        var reviewEvent = new CreateReviewEvent(Guid.NewGuid(), Guid.NewGuid(), reviewsCount: 9, isNewReview: true);
        await _notifier.TryNotifyAsync(reviewEvent, CancellationToken.None);

        await _photoProfileRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _notificationRepository.DidNotReceive()
            .TryAddPendingAsync(Arg.Any<ReviewMilestoneNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryNotifyAsync_WhenDuplicateCycle_DoesNotThrow()
    {
        var profileId = Guid.NewGuid();
        var owner = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 9002,
            Status = VipStatus.Unavaillable,
        };
        var profile = new PhotoProfile
        {
            Id = profileId,
            User = owner,
            UserId = owner.Id,
            SeasonId = Guid.NewGuid(),
            RatingCount = 10,
            Rating = 8m,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = LooksRatingApi.Domain.Vo.CityVo.Create("moscow").Value,
            AgeNomination = 22,
            GenderNomination = GenderEnum.Female,
            CreatedAt = DateTime.UtcNow,
        };

        _photoProfileRepository.GetByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _notificationRepository
            .TryAddPendingAsync(Arg.Any<ReviewMilestoneNotification>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var reviewEvent = new CreateReviewEvent(Guid.NewGuid(), profileId, reviewsCount: 10, isNewReview: true);
        await _notifier.TryNotifyAsync(reviewEvent, CancellationToken.None);

        await _notificationRepository.Received(1).TryAddPendingAsync(
            Arg.Any<ReviewMilestoneNotification>(),
            Arg.Any<CancellationToken>());
    }
}
