using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.Extensions.Logging.Abstractions;

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

        var (handler, _, _, _, _) = CreateHandler(
            reviewer,
            season,
            recommendationReturns: [profile.Id],
            profileById: new Dictionary<Guid, PhotoProfile?> { [profile.Id] = profile });

        var result = await handler.Handle(new GetUserPhotosQuery(reviewer.TelegramId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RecipientTelegramId.Should().Be(ratedUser.TelegramId);
        result.Value.SeasonTopPlace.Should().Be(12);
        result.Value.SeasonTopTotal.Should().Be(84);
    }

    [Fact]
    public async Task Handle_WhenRecommendedProfileMissingInDatabase_AddsUnviewableAndRetriesWithNextProfile()
    {
        var reviewer = CreateReviewer(70002);
        var season = Season.Create("Season 1", 1, Guid.NewGuid()).Value;
        var brokenProfileId = Guid.NewGuid();
        var validProfile = CreateDisplayableProfile(season.Id);

        var unviewableService = Substitute.For<IUnviewablePhotosProfilesService>();
        unviewableService
            .AddUnviewablePhotosProfile(brokenProfileId, reviewer.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var recommendationCall = 0;
        var recommendationService = Substitute.For<IPhotoRecommendationService>();
        recommendationService
            .GetNextUnratedProfileIdsAsync(
                reviewer.Id,
                reviewer.RecomendationSettings!.Gender,
                reviewer.RecomendationSettings.Age!.Value,
                reviewer.RecomendationSettings.City!.Value,
                Arg.Any<double?>(),
                Arg.Any<IReadOnlyCollection<Guid>?>())
            .Returns(_ =>
            {
                recommendationCall++;
                return Task.FromResult(
                    recommendationCall == 1
                        ? new List<Guid> { brokenProfileId }
                        : new List<Guid> { validProfile.Id });
            });

        var (handler, _, _, _, _) = CreateHandler(
            reviewer,
            season,
            recommendationService: recommendationService,
            unviewableService: unviewableService,
            profileById: new Dictionary<Guid, PhotoProfile?>
            {
                [brokenProfileId] = null,
                [validProfile.Id] = validProfile,
            });

        var result = await handler.Handle(new GetUserPhotosQuery(reviewer.TelegramId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileId.Should().Be(validProfile.Id);
        await unviewableService.Received(1).AddUnviewablePhotosProfile(
            brokenProfileId,
            reviewer.Id,
            Arg.Any<CancellationToken>());
        recommendationCall.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenRecommendedProfileHasNoDisplayablePhotos_AddsUnviewableAndRetriesWithNextProfile()
    {
        var reviewer = CreateReviewer(70003);
        var season = Season.Create("Season 1", 1, Guid.NewGuid()).Value;
        var emptyPhotosProfile = CreateProfileWithoutDisplayablePhotos(season.Id);
        var validProfile = CreateDisplayableProfile(season.Id);

        var unviewableService = Substitute.For<IUnviewablePhotosProfilesService>();
        unviewableService
            .AddUnviewablePhotosProfile(emptyPhotosProfile.Id, reviewer.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var recommendationCall = 0;
        var recommendationService = Substitute.For<IPhotoRecommendationService>();
        recommendationService
            .GetNextUnratedProfileIdsAsync(
                reviewer.Id,
                reviewer.RecomendationSettings!.Gender,
                reviewer.RecomendationSettings.Age!.Value,
                reviewer.RecomendationSettings.City!.Value,
                Arg.Any<double?>(),
                Arg.Any<IReadOnlyCollection<Guid>?>())
            .Returns(_ =>
            {
                recommendationCall++;
                return Task.FromResult(
                    recommendationCall == 1
                        ? new List<Guid> { emptyPhotosProfile.Id }
                        : new List<Guid> { validProfile.Id });
            });

        var (handler, _, _, _, _) = CreateHandler(
            reviewer,
            season,
            recommendationService: recommendationService,
            unviewableService: unviewableService,
            profileById: new Dictionary<Guid, PhotoProfile?>
            {
                [emptyPhotosProfile.Id] = emptyPhotosProfile,
                [validProfile.Id] = validProfile,
            });

        var result = await handler.Handle(new GetUserPhotosQuery(reviewer.TelegramId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileId.Should().Be(validProfile.Id);
        await unviewableService.Received(1).AddUnviewablePhotosProfile(
            emptyPhotosProfile.Id,
            reviewer.Id,
            Arg.Any<CancellationToken>());
        recommendationCall.Should().Be(2);
    }

    private static (
        GetUserPhotosHandler Handler,
        IUserRepository UserRepository,
        IPhotoProfileRepository PhotoProfileRepository,
        IPhotoRecommendationService RecommendationService,
        IUnviewablePhotosProfilesService UnviewableService) CreateHandler(
        User reviewer,
        Season season,
        IReadOnlyList<Guid>? recommendationReturns = null,
        IPhotoRecommendationService? recommendationService = null,
        IUnviewablePhotosProfilesService? unviewableService = null,
        IReadOnlyDictionary<Guid, PhotoProfile?>? profileById = null)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(reviewer.TelegramId).Returns(reviewer);

        var recommendation = recommendationService ?? CreateDefaultRecommendationService(reviewer, recommendationReturns);
        var unviewable = unviewableService ?? Substitute.For<IUnviewablePhotosProfilesService>();

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        if (profileById is not null)
        {
            foreach (var (profileId, profile) in profileById)
            {
                photoProfileRepository
                    .GetByIdAsync(profileId, Arg.Any<CancellationToken>())
                    .Returns(profile);
            }
        }

        var photoTopReadService = Substitute.For<IPhotoTopReadService>();
        photoTopReadService
            .GetSeasonTopPositionAsync(Arg.Any<PhotoProfile>(), season.IsClosed, Arg.Any<CancellationToken>())
            .Returns(new SeasonTopPosition(12, 84));

        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var handler = new GetUserPhotosHandler(
            userRepository,
            photoProfileRepository,
            recommendation,
            seasonRepository,
            photoTopReadService,
            unviewable,
            NullLogger<GetUserPhotosHandler>.Instance);

        return (handler, userRepository, photoProfileRepository, recommendation, unviewable);
    }

    private static IPhotoRecommendationService CreateDefaultRecommendationService(
        User reviewer,
        IReadOnlyList<Guid>? recommendationReturns)
    {
        var recommendationService = Substitute.For<IPhotoRecommendationService>();
        if (recommendationReturns is not null)
        {
            recommendationService
                .GetNextUnratedProfileIdsAsync(
                    reviewer.Id,
                    reviewer.RecomendationSettings!.Gender,
                    reviewer.RecomendationSettings.Age!.Value,
                    reviewer.RecomendationSettings.City!.Value,
                    Arg.Any<double?>(),
                    Arg.Any<IReadOnlyCollection<Guid>?>())
                .Returns(recommendationReturns.ToList());
        }

        return recommendationService;
    }

    private static PhotoProfile CreateDisplayableProfile(Guid seasonId)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 81001,
            TelegramUsername = "owner",
            Name = "Owner",
            Status = VipStatus.Unavaillable,
        };

        return new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = owner.Id,
            User = owner,
            SeasonId = seasonId,
            Rating = 8m,
            RatingCount = 5,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 18,
            GenderNomination = GenderEnum.Male,
            Photos =
            {
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    TelegramFileId = "file-valid",
                    SortOrder = 0,
                },
            },
        };
    }

    private static PhotoProfile CreateProfileWithoutDisplayablePhotos(Guid seasonId)
    {
        var profile = CreateDisplayableProfile(seasonId);
        profile.Photos.Clear();
        profile.Photos.Add(new PhotoProfilePhoto
        {
            Id = Guid.NewGuid(),
            TelegramFileId = "   ",
            SortOrder = 0,
        });
        return profile;
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
