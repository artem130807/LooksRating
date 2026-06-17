using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using LooksRatingApi.Services.PhotoProfiles;
using StackExchange.Redis;

namespace LooksRatingApi.Tests.Unit.Services.PhotoProfiles;

public sealed class PhotoProfileRatingResetServiceTests
{
    [Fact]
    public async Task ResetDatabaseAsync_ClearsProfileRatingAndDeletesReviews()
    {
        var profile = CreateProfile();
        var reviewerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository
            .GetReviewerUserIdsByPhotoProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(reviewerIds);

        var milestoneRepository = Substitute.For<IReviewMilestoneNotificationRepository>();
        var photoUserRepository = Substitute.For<IPhotoUserRepository>();
        var cache = Substitute.For<IPhotoRatingCacheService>();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        var service = new PhotoProfileRatingResetService(
            reviewRepository,
            photoUserRepository,
            milestoneRepository,
            cache,
            redis);

        var returnedReviewerIds = await service.ResetDatabaseAsync(profile, CancellationToken.None);

        profile.Rating.Should().Be(0m);
        profile.RatingCount.Should().Be(0);
        profile.Rank.Should().Be(RankEnum.Terrible);
        returnedReviewerIds.Should().BeEquivalentTo(reviewerIds);

        await reviewRepository.Received(1).GetReviewerUserIdsByPhotoProfileIdAsync(
            profile.Id,
            Arg.Any<CancellationToken>());
        await milestoneRepository.Received(1).DeletePendingByPhotoProfileIdAsync(
            profile.Id,
            Arg.Any<CancellationToken>());
        await reviewRepository.Received(1).DeleteByPhotoProfileIdAsync(
            profile.Id,
            Arg.Any<CancellationToken>());
        await photoUserRepository.Received(1).ResetLegacyRatingsForProfileAsync(
            profile.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetCacheAsync_ClearsRatedMarkersAndRedisRating()
    {
        var profile = CreateProfile();
        var reviewerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var reviewRepository = Substitute.For<IReviewRepository>();
        var milestoneRepository = Substitute.For<IReviewMilestoneNotificationRepository>();
        var photoUserRepository = Substitute.For<IPhotoUserRepository>();
        var cache = Substitute.For<IPhotoRatingCacheService>();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        var service = new PhotoProfileRatingResetService(
            reviewRepository,
            photoUserRepository,
            milestoneRepository,
            cache,
            redis);

        await service.ResetCacheAsync(
            profile,
            PhotoProfileNomination.From(profile),
            reviewerIds,
            CancellationToken.None);

        await cache.Received(1).ClearRatedMarkersForProfileAsync(
            profile.Id,
            profile.SeasonId,
            reviewerIds,
            Arg.Any<CancellationToken>());
        await cache.Received(1).ResetProfileRatingAsync(
            profile.Id,
            profile.SeasonId,
            "moscow",
            "moscow",
            Arg.Any<CancellationToken>());
        await database.Received(1).KeyDeleteAsync(
            ReviewRedisKeys.SequenceCount(profile.Id),
            Arg.Any<CommandFlags>());
    }

    private static PhotoProfile CreateProfile()
    {
        return new PhotoProfile
        {
            Id = Guid.NewGuid(),
            SeasonId = Guid.NewGuid(),
            Rating = 8m,
            RatingCount = 5,
            Rank = RankEnum.Cute,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Male,
            Status = StatusEnum.Active,
        };
    }
}
