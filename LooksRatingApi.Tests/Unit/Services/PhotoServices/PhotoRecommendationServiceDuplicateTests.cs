using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.Services.PhotoServices;

public sealed class PhotoRecommendationServiceDuplicateTests
{
    [Fact]
    public async Task SequentialScroll_WithOnlyValidProfiles_DoesNotRepeatBeforeCycleCompletes()
    {
        var (reviewer, season, profiles) = CreateReviewerAndFeed(profileCount: 5);
        var feedCycleStore = new InMemoryFeedCycleStore();
        var service = CreateRecommendationService(season, profiles, feedCycleStore);

        var seen = new HashSet<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var result = await service.GetNextUnratedProfileIdsAsync(
                reviewer.Id,
                GenderEnum.Male,
                age: 25,
                city: "moscow");

            result.Should().ContainSingle();
            seen.Add(result[0]).Should().BeTrue($"profile {result[0]} repeated on step {i + 1}");
        }
    }

    [Fact]
    public async Task SkipProfileIds_WhenAllRemainingSkipped_DoesNotResetCycleOrReturnDuplicate()
    {
        var (reviewer, season, profiles) = CreateReviewerAndFeed(profileCount: 3);
        var feedCycleStore = new InMemoryFeedCycleStore();
        var service = CreateRecommendationService(season, profiles, feedCycleStore);

        var firstProfileId = profiles[0].Id;
        await feedCycleStore.TryMarkProfileAsServedAsync(reviewer.Id, season.Id, firstProfileId);

        var result = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow",
            skipProfileIds: new[] { profiles[1].Id, profiles[2].Id });

        result.Should().BeEmpty();
        feedCycleStore.SnapshotRated(reviewer.Id, season.Id)
            .Should().ContainSingle()
            .Which.Should().Be(firstProfileId);
    }

    [Fact]
    public async Task Handler_WithWhitespaceOnlyPhoto_DoesNotRepeatPreviouslySeenProfile()
    {
        var (reviewer, season, validProfiles) = CreateReviewerAndFeed(profileCount: 2);
        var brokenProfile = CreateWhitespacePhotoProfile(season);
        var allProfiles = validProfiles.Append(brokenProfile).ToList();

        var feedCycleStore = new InMemoryFeedCycleStore();
        var recommendationService = CreateRecommendationService(season, allProfiles, feedCycleStore);
        var handler = CreateGetUserPhotosHandler(season, reviewer, allProfiles, recommendationService);

        var first = await handler.Handle(new GetUserPhotosQuery(reviewer.TelegramId), CancellationToken.None);
        first.IsSuccess.Should().BeTrue();
        var firstProfileId = first.Value.ProfileId;

        var second = await handler.Handle(new GetUserPhotosQuery(reviewer.TelegramId), CancellationToken.None);
        second.IsSuccess.Should().BeTrue();

        second.Value.ProfileId.Should().NotBe(
            firstProfileId,
            "whitespace-only profiles are excluded from feed, so the second card must be a different valid profile");
    }

    [Fact]
    public void WhitespaceOnlyPhoto_IsExcludedFromFeedQuery_AndRejectedByHandler()
    {
        var (_, season, validProfiles) = CreateReviewerAndFeed(profileCount: 1);
        var brokenProfile = CreateWhitespacePhotoProfile(season);
        var catalog = new TestFeedCatalog(validProfiles.Append(brokenProfile));

        catalog.CountFeed(season.Id, reviewerUserId: Guid.NewGuid(), cityNomination: "moscow", gender: GenderEnum.Male, age: 25, excludeProfileIds: null)
            .Should().Be(1, "feed query excludes whitespace-only TelegramFileId");

        brokenProfile.Photos
            .Where(photo => !string.IsNullOrWhiteSpace(photo.TelegramFileId))
            .Should()
            .BeEmpty("handler rejects whitespace-only TelegramFileId");
    }

    [Fact]
    public async Task RedisRatedSetLoss_AllowsPreviouslyServedButUnreviewedProfileToReappear()
    {
        var (reviewer, season, profiles) = CreateReviewerAndFeed(profileCount: 1);
        var feedCycleStore = new InMemoryFeedCycleStore();
        var service = CreateRecommendationService(season, profiles, feedCycleStore);

        var first = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");
        first.Should().ContainSingle();

        await feedCycleStore.ResetCycleAsync(reviewer.Id, season.Id, DateTime.UtcNow);

        var second = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");

        second.Should().ContainSingle();
        second[0].Should().Be(first[0], "lost rated set allows the same profile again");
    }

    [Fact]
    public async Task ManyUnviewableProfiles_AcceleratesCycleRestart_AndRepeatsSeenProfile()
    {
        var (reviewer, season, profiles) = CreateReviewerAndFeed(profileCount: 5);
        var feedCycleStore = new InMemoryFeedCycleStore();

        var unviewable = Substitute.For<IUnviewablePhotosProfilesService>();
        unviewable.GetUnviewablePhotosProfile(reviewer.Id)
            .Returns(Result.Success(profiles.Skip(1).Select(p => p.Id).ToList()));

        var service = CreateRecommendationService(season, profiles, feedCycleStore, unviewable);

        var first = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");
        first.Should().ContainSingle();
        first[0].Should().Be(profiles[0].Id);

        var second = await service.GetNextUnratedProfileIdsAsync(
            reviewer.Id,
            GenderEnum.Male,
            age: 25,
            city: "moscow");

        second.Should().ContainSingle();
        second[0].Should().Be(profiles[0].Id, "only one viewable profile left in nomination, cycle restarts immediately");
    }

    private static PhotoRecommendationService CreateRecommendationService(
        Season season,
        IReadOnlyList<PhotoProfile> profiles,
        InMemoryFeedCycleStore feedCycleStore,
        IUnviewablePhotosProfilesService? unviewableService = null)
    {
        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var cityService = Substitute.For<ICityService>();
        cityService
            .TryResolveCanonicalCity(Arg.Any<string>(), out Arg.Any<string?>())
            .Returns(callInfo =>
            {
                callInfo[1] = "moscow";
                return true;
            });

        var unviewable = unviewableService ?? Substitute.For<IUnviewablePhotosProfilesService>();
        if (unviewableService is null)
        {
            unviewable.GetUnviewablePhotosProfile(Arg.Any<Guid>())
                .Returns(Result.Success(new List<Guid>()));
        }

        return new PhotoRecommendationService(
            feedCycleStore,
            new NormalizeCityNameService(),
            cityService,
            seasonRepository,
            CreateFeedRepository(profiles),
            Substitute.For<IReviewRepository>(),
            unviewable,
            NullLogger<PhotoRecommendationService>.Instance);
    }

    private static GetUserPhotosHandler CreateGetUserPhotosHandler(
        Season season,
        User reviewer,
        IReadOnlyList<PhotoProfile> profiles,
        IPhotoRecommendationService recommendationService)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(reviewer.TelegramId).Returns(reviewer);

        var unviewableService = Substitute.For<IUnviewablePhotosProfilesService>();
        unviewableService
            .AddUnviewablePhotosProfile(Arg.Any<Guid>(), reviewer.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        unviewableService
            .GetUnviewablePhotosProfile(reviewer.Id)
            .Returns(Result.Success(new List<Guid>()));

        var photoTopReadService = Substitute.For<IPhotoTopReadService>();
        photoTopReadService
            .GetSeasonTopPositionAsync(Arg.Any<PhotoProfile>(), season.IsClosed, Arg.Any<CancellationToken>())
            .Returns(new SeasonTopPosition(1, 10));

        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        return new GetUserPhotosHandler(
            userRepository,
            CreateFeedRepository(profiles),
            recommendationService,
            seasonRepository,
            photoTopReadService,
            unviewableService,
            NullLogger<GetUserPhotosHandler>.Instance);
    }

    private static IPhotoProfileRepository CreateFeedRepository(IReadOnlyList<PhotoProfile> profiles)
    {
        var catalog = new TestFeedCatalog(profiles);
        var repository = Substitute.For<IPhotoProfileRepository>();

        repository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.Arg<Guid>();
                return catalog.FindById(id);
            });

        repository
            .CountFeedProfilesAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<GenderEnum>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyCollection<Guid>?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var seasonId = callInfo.ArgAt<Guid>(0);
                var reviewerUserId = callInfo.ArgAt<Guid>(1);
                var city = callInfo.ArgAt<string>(2);
                var gender = callInfo.ArgAt<GenderEnum>(3);
                var age = callInfo.ArgAt<int>(4);
                var exclude = callInfo.ArgAt<IReadOnlyCollection<Guid>?>(5);
                return Task.FromResult(catalog.CountFeed(seasonId, reviewerUserId, city, gender, age, exclude));
            });

        repository
            .GetRandomFeedCandidateProfileIdsAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<GenderEnum>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var seasonId = callInfo.ArgAt<Guid>(0);
                var reviewerUserId = callInfo.ArgAt<Guid>(1);
                var city = callInfo.ArgAt<string>(2);
                var gender = callInfo.ArgAt<GenderEnum>(3);
                var age = callInfo.ArgAt<int>(4);
                var take = callInfo.ArgAt<int>(5);
                var exclude = callInfo.ArgAt<IReadOnlyCollection<Guid>>(6);
                var vipOnly = callInfo.ArgAt<bool>(7);
                return Task.FromResult(catalog.GetCandidates(seasonId, reviewerUserId, city, gender, age, take, exclude, vipOnly));
            });

        repository
            .GetRandomNewFeedCandidateProfileIdsAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<GenderEnum>(),
                Arg.Any<int>(),
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var seasonId = callInfo.ArgAt<Guid>(0);
                var reviewerUserId = callInfo.ArgAt<Guid>(1);
                var city = callInfo.ArgAt<string>(2);
                var gender = callInfo.ArgAt<GenderEnum>(3);
                var age = callInfo.ArgAt<int>(4);
                var createdAfter = callInfo.ArgAt<DateTime>(5);
                var take = callInfo.ArgAt<int>(6);
                var exclude = callInfo.ArgAt<IReadOnlyCollection<Guid>>(7);
                var vipOnly = callInfo.ArgAt<bool>(8);
                return Task.FromResult(catalog.GetNewCandidates(
                    seasonId,
                    reviewerUserId,
                    city,
                    gender,
                    age,
                    createdAfter,
                    take,
                    exclude,
                    vipOnly));
            });

        return repository;
    }

    private static (User Reviewer, Season Season, List<PhotoProfile> Profiles) CreateReviewerAndFeed(int profileCount)
    {
        var season = Season.Create("Test season", 1, Guid.NewGuid()).Value;
        var reviewerId = Guid.NewGuid();
        var reviewer = new User
        {
            Id = reviewerId,
            TelegramId = 88001,
            TelegramUsername = "reviewer_88001",
            Name = "Reviewer",
            Status = VipStatus.Unavaillable,
            RecomendationSettings = RecomendationSettings.Create(
                25,
                GenderEnum.Male,
                CityVo.Create("moscow").Value,
                reviewerId).Value,
        };

        var profiles = new List<PhotoProfile>();
        for (var i = 0; i < profileCount; i++)
        {
            profiles.Add(CreateValidFeedProfile(season, telegramId: 88100 + i));
        }

        return (reviewer, season, profiles);
    }

    private static PhotoProfile CreateValidFeedProfile(Season season, long telegramId)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = VipStatus.Unavaillable,
        };

        return new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = owner.Id,
            User = owner,
            SeasonId = season.Id,
            Rating = 7m,
            RatingCount = 5,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Male,
            CreatedAt = DateTime.UtcNow,
            Photos =
            {
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    TelegramFileId = $"file-{telegramId}",
                    SortOrder = 0,
                },
            },
        };
    }

    private static PhotoProfile CreateWhitespacePhotoProfile(Season season)
    {
        var profile = CreateValidFeedProfile(season, telegramId: 88999);
        profile.Photos.Single().TelegramFileId = "   ";
        return profile;
    }

    private sealed class TestFeedCatalog
    {
        private readonly IReadOnlyList<PhotoProfile> _profiles;

        public TestFeedCatalog(IEnumerable<PhotoProfile> profiles)
        {
            _profiles = profiles.ToList();
        }

        public Task<PhotoProfile?> FindById(Guid id) =>
            Task.FromResult(_profiles.FirstOrDefault(profile => profile.Id == id));

        public int CountFeed(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            IReadOnlyCollection<Guid>? excludeProfileIds) =>
            Filter(seasonId, reviewerUserId, cityNomination, gender, age, excludeProfileIds, vipOnly: false).Count();

        public List<Guid> GetCandidates(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            int take,
            IReadOnlyCollection<Guid> excludeProfileIds,
            bool vipOnly) =>
            Filter(seasonId, reviewerUserId, cityNomination, gender, age, excludeProfileIds, vipOnly)
                .OrderBy(profile => profile.Id)
                .Take(take)
                .Select(profile => profile.Id)
                .ToList();

        public List<Guid> GetNewCandidates(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            DateTime createdAfter,
            int take,
            IReadOnlyCollection<Guid> excludeProfileIds,
            bool vipOnly) =>
            Filter(seasonId, reviewerUserId, cityNomination, gender, age, excludeProfileIds, vipOnly)
                .Where(profile => profile.CreatedAt > createdAfter)
                .OrderBy(profile => profile.Id)
                .Take(take)
                .Select(profile => profile.Id)
                .ToList();

        private IEnumerable<PhotoProfile> Filter(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            IReadOnlyCollection<Guid>? excludeProfileIds,
            bool vipOnly)
        {
            var exclude = excludeProfileIds?.ToHashSet() ?? new HashSet<Guid>();
            return _profiles.Where(profile =>
                profile.SeasonId == seasonId
                && profile.Status == StatusEnum.Active
                && profile.UserId != reviewerUserId
                && profile.CityNomination.Value == cityNomination
                && profile.Photos.Any(photo => !string.IsNullOrWhiteSpace(photo.TelegramFileId))
                && (!vipOnly || profile.User.Status == VipStatus.Availlable)
                && GenderFeedHelper.Matches(gender, profile.GenderNomination)
                && TopService.MatchesAge(age, profile.AgeNomination)
                && !exclude.Contains(profile.Id));
        }
    }
}
