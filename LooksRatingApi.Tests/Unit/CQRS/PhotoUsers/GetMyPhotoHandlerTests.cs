using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Tests.Unit.CQRS.PhotoUsers;

public sealed class GetMyPhotoHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSeasonTopPositionInResponse()
    {
        var seasonId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 88001,
            TelegramUsername = "artem",
            Name = "Artem",
            Status = VipStatus.Unavaillable,
        };
        var season = Season.Create("Season 1", 1, Guid.NewGuid()).Value;
        season.Id = seasonId;

        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SeasonId = seasonId,
            Rating = 10m,
            RatingCount = 1,
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
                    TelegramFileId = "file-1",
                    SortOrder = 0,
                },
            },
        };

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByTelegramId(88001).Returns(user);

        var seasonRepository = Substitute.For<ISeasonRepository>();
        seasonRepository.GetCurrent().Returns(season);

        var photoProfileRepository = Substitute.For<IPhotoProfileRepository>();
        photoProfileRepository
            .GetByUserAndSeasonAsync(user.Id, seasonId, Arg.Any<CancellationToken>())
            .Returns(profile);
        photoProfileRepository
            .GetSeasonTopPositionAsync(profile, season.IsClosed, Arg.Any<CancellationToken>())
            .Returns(new SeasonTopPosition(12, 84));

        var handler = new GetMyPhotoHandler(userRepository, photoProfileRepository, seasonRepository);

        var result = await handler.Handle(new GetMyPhotoQuery(88001), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeasonTopPlace.Should().Be(12);
        result.Value.SeasonTopTotal.Should().Be(84);
    }
}
