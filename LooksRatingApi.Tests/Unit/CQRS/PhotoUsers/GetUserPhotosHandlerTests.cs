using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Tests.Unit.CQRS.PhotoUsers;

public sealed class GetUserPhotosHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSeasonTopPositionInResponse()
    {
        var seasonId = Guid.NewGuid();
        var reviewer = CreateReviewer(70001);
        var ratedUser = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 80001,
            TelegramUsername = "rated",
            Name = "Rated User",
            Status = VipStatus.Unavaillable,
        };
        var season = Season.Create("Season 1", 1, Guid.NewGuid()).Value;
        season.Id = seasonId;

        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = ratedUser.Id,
            User = ratedUser,
            SeasonId = seasonId,
            Rating = 9.5m,
            RatingCount = 12,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("ulyanovsk").Value,
            AgeNomination = 18,
            GenderNomination = GenderEnum.Male,
            Photos =
            {
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    TelegramFileId = "file-rated",
                    SortOrder = 0,
                },
            },
        };

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(reviewer.TelegramId).Returns(reviewer);

        var recommendationService = Substitute.For<IPhotoRecommendationService>();
        recommendationService
            .GetNextUnratedProfileIdsAsync(
                reviewer.Id,
                reviewer.RecomendationSettings!.Gender,
                reviewer.RecomendationSettings.Age!.Value,
                reviewer.RecomendationSettings.City!.Value,
                Arg.Any<double?>(),
                Arg.Any<IReadOnlyCollection<Guid>?>())
            .Returns([profile.Id]);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository
            .GetByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);

        var photoTopReadService = Substitute.For<IPhotoTopReadService>();
        photoTopReadService
            .GetSeasonTopPositionAsync(profile, season.IsClosed, Arg.Any<CancellationToken>())
            .Returns(new SeasonTopPosition(12, 84));

        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var handler = new GetUserPhotosHandler(
            userRepository,
            photoProfileRepository,
            recommendationService,
            seasonRepository,
            photoTopReadService);

        var result = await handler.Handle(new GetUserPhotosQuery(reviewer.TelegramId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeasonTopPlace.Should().Be(12);
        result.Value.SeasonTopTotal.Should().Be(84);
    }

    private static User CreateReviewer(long telegramId)
    {
        var userId = Guid.NewGuid();
        var settings = RecomendationSettings.Create(
            18,
            GenderEnum.Male,
            CityVo.Create("moscow").Value,
            userId).Value;

        return new User
        {
            Id = userId,
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = VipStatus.Unavaillable,
            RecomendationSettings = settings,
        };
    }
}
