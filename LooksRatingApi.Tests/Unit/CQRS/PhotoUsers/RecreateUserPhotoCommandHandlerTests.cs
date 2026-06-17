using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services.CityServices;
using LooksRatingApi.Services.PhotoProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LooksRatingApi.Tests.Unit.CQRS.PhotoUsers;

public sealed class RecreateUserPhotoCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenNonVipReplacesPhotoWithSameNomination_ResetsRating()
    {
        var profile = CreateProfile(VipStatus.Unavaillable);
        var handler = CreateHandler(profile, out var resetService);

        var result = await handler.Handle(
            CreateCommand(profile, city: "moscow"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(0m);
        result.Value.RatingCount.Should().Be(0);
        await resetService.Received(1).ResetDatabaseAsync(
            Arg.Is<PhotoProfile>(x => x.Id == profile.Id),
            Arg.Any<CancellationToken>());
        await resetService.Received(1).ResetCacheAsync(
            Arg.Is<PhotoProfile>(x => x.Id == profile.Id),
            Arg.Is<PhotoProfileNomination>(x => x.City == "moscow"),
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVipReplacesPhotoWithSameNomination_KeepsRating()
    {
        var profile = CreateProfile(VipStatus.Availlable);
        var handler = CreateHandler(profile, out var resetService);

        var result = await handler.Handle(
            CreateCommand(
                profile,
                city: "moscow",
                targetPhotoId: profile.Photos.Last().Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(7.5m);
        result.Value.RatingCount.Should().Be(10);
        await resetService.DidNotReceive().ResetDatabaseAsync(
            Arg.Any<PhotoProfile>(),
            Arg.Any<CancellationToken>());
        await resetService.DidNotReceive().ResetCacheAsync(
            Arg.Any<PhotoProfile>(),
            Arg.Any<PhotoProfileNomination>(),
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVipChangesCity_ResetsRating()
    {
        var profile = CreateProfile(VipStatus.Availlable, photoCount: 3);
        var handler = CreateHandler(profile, out var resetService);

        var result = await handler.Handle(
            CreateCommand(profile, city: "spb", targetPhotoId: profile.Photos.Skip(1).First().Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(0m);
        result.Value.RatingCount.Should().Be(0);
        result.Value.City.Should().Be("spb");
        await resetService.Received(1).ResetDatabaseAsync(
            Arg.Is<PhotoProfile>(x => x.Id == profile.Id),
            Arg.Any<CancellationToken>());
        await resetService.Received(1).ResetCacheAsync(
            Arg.Is<PhotoProfile>(x => x.Id == profile.Id),
            Arg.Is<PhotoProfileNomination>(x => x.City == "moscow"),
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    private static RecreateUserPhotoCommand CreateCommand(
        PhotoProfile profile,
        string city,
        Guid? targetPhotoId = null)
    {
        return new RecreateUserPhotoCommand(new RecreateUserPhotoRequest
        {
            TelegramId = profile.User.TelegramId,
            TelegramFileId = "new-file-id",
            TargetPhotoId = targetPhotoId,
            Nomination = new PhotoNominationRequest
            {
                Age = profile.AgeNomination,
                Gender = profile.GenderNomination,
                City = city,
            },
        });
    }

    private static PhotoProfile CreateProfile(VipStatus vipStatus, int photoCount = 1)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 8000 + photoCount,
            TelegramUsername = "owner",
            Status = vipStatus,
        };

        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            SeasonId = Guid.NewGuid(),
            Rating = 7.5m,
            RatingCount = 10,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Male,
            CreatedAt = DateTime.UtcNow,
        };

        for (var i = 0; i < photoCount; i++)
        {
            profile.Photos.Add(new PhotoProfilePhoto
            {
                Id = Guid.NewGuid(),
                PhotoProfileId = profile.Id,
                TelegramFileId = $"file-{i}",
                SortOrder = i,
                CreatedAt = DateTime.UtcNow,
            });
        }

        return profile;
    }

    private static RecreateUserPhotoCommandHandler CreateHandler(
        PhotoProfile profile,
        out IPhotoProfileRatingResetService resetService)
    {
        var cityService = Substitute.For<ICityService>();
        cityService.IsCityValid(Arg.Any<string>()).Returns(true);

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(profile.User.TelegramId).Returns(profile.User);

        var season = new Season
        {
            Id = profile.SeasonId,
            Name = "Season",
            Number = 1,
            IsClosed = false,
            CreatedDate = DateTime.UtcNow,
            ListSeasonsId = Guid.NewGuid(),
        };

        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository
            .GetByUserAndSeasonAsync(profile.UserId, profile.SeasonId, Arg.Any<CancellationToken>())
            .Returns(profile);
        photoProfileRepository
            .UpdateAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var updated = callInfo.Arg<PhotoProfile>();
                if (PhotoProfileRatingResetPolicy.ShouldResetRating(
                        profile.User.Status,
                        PhotoProfileNomination.From(profile),
                        PhotoProfileNomination.From(
                            updated.AgeNomination,
                            updated.GenderNomination,
                            updated.CityNomination.Value ?? string.Empty)))
                {
                    updated.ResetRatings();
                }

                return Task.CompletedTask;
            });

        var validator = Substitute.For<IRecreateUserPhotoValidator>();
        validator.ValidateAsync(Arg.Any<RecreateUserPhotoCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(string.Empty));

        resetService = Substitute.For<IPhotoProfileRatingResetService>();
        resetService
            .ResetDatabaseAsync(Arg.Any<PhotoProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<PhotoProfile>().ResetRatings();
                return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
            });
        resetService
            .ResetCacheAsync(
                Arg.Any<PhotoProfile>(),
                Arg.Any<PhotoProfileNomination>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var context = CreateContext();

        return new RecreateUserPhotoCommandHandler(
            userRepository,
            photoProfileRepository,
            validator,
            seasonRepository,
            cityService,
            new NormalizeCityNameService(),
            resetService,
            context);
    }

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
