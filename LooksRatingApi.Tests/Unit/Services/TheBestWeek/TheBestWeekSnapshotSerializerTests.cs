using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services.TheBestWeek;

namespace LooksRatingApi.Tests.Unit.Services.TheBestWeek;

public sealed class TheBestWeekSnapshotSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsProfileFields()
    {
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var profile = new PhotoProfile
        {
            Id = profileId,
            UserId = userId,
            User = new User
            {
                Id = userId,
                TelegramId = 9001,
                TelegramUsername = "winner",
                Name = "Winner",
            },
            CityNomination = CityVo.Create("moscow").Value,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Male,
            Rating = 9.2m,
            RatingCount = 42,
            Rank = RankEnum.Cute,
            Status = StatusEnum.Active,
            CreatedAt = new DateTime(2026, 5, 20, 8, 0, 0, DateTimeKind.Utc),
            Photos =
            [
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    PhotoProfileId = profileId,
                    TelegramFileId = "file-a",
                    SortOrder = 0,
                },
                new PhotoProfilePhoto
                {
                    Id = Guid.NewGuid(),
                    PhotoProfileId = profileId,
                    TelegramFileId = "file-b",
                    SortOrder = 1,
                },
            ],
        };

        var json = TheBestWeekSnapshotSerializer.Serialize([profile]);
        var items = TheBestWeekSnapshotSerializer.Deserialize(json);
        var restored = TheBestWeekSnapshotSerializer.ToProfile(items[0]);

        restored.Id.Should().Be(profileId);
        restored.UserId.Should().Be(userId);
        restored.User!.TelegramId.Should().Be(9001);
        restored.CityNomination.Value.Should().Be("moscow");
        restored.Rating.Should().Be(9.2m);
        restored.Photos.Select(photo => photo.TelegramFileId).Should().Equal("file-a", "file-b");
    }

    [Fact]
    public void Deserialize_LegacyPhotoUserPayload_IsSupported()
    {
        const string legacyJson =
            """
            [
              {
                "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                "userId": "11111111-2222-3333-4444-555555555555",
                "user": {
                  "telegramId": 7001,
                  "telegramUsername": "legacy",
                  "name": "Legacy User"
                },
                "cityNomination": { "value": "spb" },
                "ageNomination": 30,
                "genderNomination": 1,
                "rating": 8.5,
                "ratingCount": 12,
                "rank": 2,
                "createdAt": "2026-01-15T00:00:00Z",
                "telegramFileId": "legacy-file"
              }
            ]
            """;

        var items = TheBestWeekSnapshotSerializer.Deserialize(legacyJson);

        items.Should().ContainSingle();
        items[0].TelegramId.Should().Be(7001);
        items[0].City.Should().Be("spb");
        items[0].TelegramFileIds.Should().Equal("legacy-file");
    }
}
