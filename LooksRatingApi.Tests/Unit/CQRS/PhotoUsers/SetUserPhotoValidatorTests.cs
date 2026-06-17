using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Tests.Unit.CQRS.PhotoUsers;

public sealed class SetUserPhotoValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WhenVipAlreadyHasFourPhotos_ReturnsVipPhotoLimitExceeded()
    {
        var user = CreateUser(VipStatus.Availlable);
        var profile = CreateProfile(user, photoCount: 4);

        var validator = CreateValidator(user, profile);
        var result = await validator.ValidateAsync(
            new SetUserPhotoCommand(new SetUserPhotoRequest
            {
                TelegramId = user.TelegramId,
                TelegramFileId = "file-new",
            }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SetUserPhotoErrors.VipPhotoLimitExceeded);
    }

    [Fact]
    public async Task ValidateAsync_WhenVipHasThreePhotos_AllowsFourth()
    {
        var user = CreateUser(VipStatus.Availlable);
        var profile = CreateProfile(user, photoCount: 3);

        var validator = CreateValidator(user, profile);
        var result = await validator.ValidateAsync(
            new SetUserPhotoCommand(new SetUserPhotoRequest
            {
                TelegramId = user.TelegramId,
                TelegramFileId = "file-new",
            }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenNonVipAlreadyHasPhoto_ReturnsPhotoAlreadyExists()
    {
        var user = CreateUser(VipStatus.Unavaillable);
        var profile = CreateProfile(user, photoCount: 1);

        var validator = CreateValidator(user, profile);
        var result = await validator.ValidateAsync(
            new SetUserPhotoCommand(new SetUserPhotoRequest
            {
                TelegramId = user.TelegramId,
                TelegramFileId = "file-new",
            }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SetUserPhotoErrors.PhotoAlreadyExists);
    }

    private static SetUserPhotoValidator CreateValidator(User user, PhotoProfile profile)
    {
        var season = new Season
        {
            Id = profile.SeasonId,
            Name = "Season",
            Number = 1,
            IsClosed = false,
            CreatedDate = DateTime.UtcNow,
            ListSeasonsId = Guid.NewGuid(),
        };

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(user.TelegramId).Returns(user);

        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository
            .GetByUserAndSeasonAsync(user.Id, season.Id, Arg.Any<CancellationToken>())
            .Returns(profile);

        return new SetUserPhotoValidator(userRepository, photoProfileRepository, seasonRepository);
    }

    private static User CreateUser(VipStatus vipStatus) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = 42_001,
        TelegramUsername = "vip-user",
        Status = vipStatus,
    };

    private static PhotoProfile CreateProfile(User user, int photoCount)
    {
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            SeasonId = Guid.NewGuid(),
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
}
