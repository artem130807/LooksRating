using System.Text.Json;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services.TheBestWeek
{
    public sealed class TheBestWeekSnapshotEnvelope
    {
        public int Version { get; set; } = 1;
        public List<TheBestWeekSnapshotItem> Items { get; set; } = new();
    }

    public sealed class TheBestWeekSnapshotItem
    {
        public Guid ProfileId { get; set; }
        public Guid UserId { get; set; }
        public long TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? DisplayName { get; set; }
        public string City { get; set; } = string.Empty;
        public int AgeNomination { get; set; }
        public GenderEnum GenderNomination { get; set; }
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public string Rank { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> TelegramFileIds { get; set; } = new();
    }

    internal sealed class LegacySnapshotPhotoProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public LegacySnapshotUser? User { get; set; }
        public LegacySnapshotCity? CityNomination { get; set; }
        public int AgeNomination { get; set; }
        public GenderEnum GenderNomination { get; set; }
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public RankEnum Rank { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TelegramFileId { get; set; }
        public List<LegacySnapshotProfilePhoto>? Photos { get; set; }
    }

    internal sealed class LegacySnapshotPhotoUser
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public LegacySnapshotUser? User { get; set; }
        public LegacySnapshotCity? CityNomination { get; set; }
        public int AgeNomination { get; set; }
        public GenderEnum GenderNomination { get; set; }
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public RankEnum Rank { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TelegramFileId { get; set; }
    }

    internal sealed class LegacySnapshotProfilePhoto
    {
        public string? TelegramFileId { get; set; }
        public int SortOrder { get; set; }
    }

    internal sealed class LegacySnapshotUser
    {
        public long TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? Name { get; set; }
    }

    internal sealed class LegacySnapshotCity
    {
        public string? Value { get; set; }
    }

    public static class TheBestWeekSnapshotSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static string Serialize(IEnumerable<PhotoProfile> profiles)
        {
            var envelope = new TheBestWeekSnapshotEnvelope
            {
                Version = 1,
                Items = profiles
                    .Select(ToSnapshotItem)
                    .ToList()
            };

            return JsonSerializer.Serialize(envelope, Options);
        }

        public static List<TheBestWeekSnapshotItem> Deserialize(string? snapshotJson)
        {
            if (string.IsNullOrWhiteSpace(snapshotJson))
            {
                return new List<TheBestWeekSnapshotItem>();
            }

            var envelope = TryDeserialize<TheBestWeekSnapshotEnvelope>(snapshotJson);
            if (envelope?.Items is { Count: > 0 })
            {
                return envelope.Items;
            }

            var directItems = TryDeserialize<List<TheBestWeekSnapshotItem>>(snapshotJson);
            if (directItems is { Count: > 0 })
            {
                return directItems;
            }

            var legacyProfiles = TryDeserialize<List<LegacySnapshotPhotoProfile>>(snapshotJson);
            if (legacyProfiles is { Count: > 0 })
            {
                return legacyProfiles.Select(ToSnapshotItem).ToList();
            }

            var legacyPhotoUsers = TryDeserialize<List<LegacySnapshotPhotoUser>>(snapshotJson);
            if (legacyPhotoUsers is { Count: > 0 })
            {
                return legacyPhotoUsers.Select(ToSnapshotItem).ToList();
            }

            return new List<TheBestWeekSnapshotItem>();
        }

        public static PhotoProfile ToProfile(TheBestWeekSnapshotItem item)
        {
            var cityResult = CityVo.Create(item.City);
            if (cityResult.IsFailure)
            {
                cityResult = CityVo.Create("unknown");
            }

            _ = Enum.TryParse<RankEnum>(item.Rank, true, out var rank);

            var profile = new PhotoProfile
            {
                Id = item.ProfileId,
                UserId = item.UserId,
                User = new User
                {
                    Id = item.UserId,
                    TelegramId = item.TelegramId,
                    TelegramUsername = item.TelegramUsername,
                    Name = item.DisplayName
                },
                CityNomination = cityResult.Value,
                AgeNomination = item.AgeNomination,
                GenderNomination = item.GenderNomination,
                Rating = item.Rating,
                RatingCount = item.RatingCount,
                Rank = rank,
                Status = StatusEnum.Active,
                CreatedAt = item.CreatedAt == default ? DateTime.UtcNow : item.CreatedAt,
                Photos = item.TelegramFileIds
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select((fileId, index) => new PhotoProfilePhoto
                    {
                        Id = Guid.NewGuid(),
                        PhotoProfileId = item.ProfileId,
                        TelegramFileId = fileId,
                        SortOrder = index
                    })
                    .ToList()
            };

            return profile;
        }

        private static TheBestWeekSnapshotItem ToSnapshotItem(PhotoProfile profile)
        {
            var files = profile.Photos
                .OrderBy(x => x.SortOrder)
                .Select(x => x.TelegramFileId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return new TheBestWeekSnapshotItem
            {
                ProfileId = profile.Id,
                UserId = profile.UserId,
                TelegramId = profile.User?.TelegramId ?? 0,
                TelegramUsername = profile.User?.TelegramUsername,
                DisplayName = UserPublicDisplayName.Resolve(profile.User),
                City = profile.CityNomination?.Value ?? string.Empty,
                AgeNomination = profile.AgeNomination,
                GenderNomination = profile.GenderNomination,
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
                Rank = profile.Rank.ToString(),
                CreatedAt = profile.CreatedAt,
                TelegramFileIds = files
            };
        }

        private static TheBestWeekSnapshotItem ToSnapshotItem(LegacySnapshotPhotoProfile profile)
        {
            var files = profile.Photos?
                .OrderBy(x => x.SortOrder)
                .Select(x => x.TelegramFileId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList() ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(profile.TelegramFileId))
            {
                files.Insert(0, profile.TelegramFileId);
            }

            return new TheBestWeekSnapshotItem
            {
                ProfileId = profile.Id,
                UserId = profile.UserId,
                TelegramId = profile.User?.TelegramId ?? 0,
                TelegramUsername = profile.User?.TelegramUsername,
                DisplayName = !string.IsNullOrWhiteSpace(profile.User?.Name)
                    ? profile.User!.Name
                    : string.IsNullOrWhiteSpace(profile.User?.TelegramUsername)
                        ? UserPublicDisplayName.DefaultParticipant
                        : $"@{profile.User!.TelegramUsername!.TrimStart('@')}",
                City = profile.CityNomination?.Value ?? string.Empty,
                AgeNomination = profile.AgeNomination,
                GenderNomination = profile.GenderNomination,
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
                Rank = profile.Rank.ToString(),
                CreatedAt = profile.CreatedAt,
                TelegramFileIds = files
            };
        }

        private static TheBestWeekSnapshotItem ToSnapshotItem(LegacySnapshotPhotoUser photoUser)
        {
            var files = new List<string>();
            if (!string.IsNullOrWhiteSpace(photoUser.TelegramFileId))
            {
                files.Add(photoUser.TelegramFileId);
            }

            return new TheBestWeekSnapshotItem
            {
                ProfileId = photoUser.Id,
                UserId = photoUser.UserId,
                TelegramId = photoUser.User?.TelegramId ?? 0,
                TelegramUsername = photoUser.User?.TelegramUsername,
                DisplayName = !string.IsNullOrWhiteSpace(photoUser.User?.Name)
                    ? photoUser.User!.Name
                    : string.IsNullOrWhiteSpace(photoUser.User?.TelegramUsername)
                        ? UserPublicDisplayName.DefaultParticipant
                        : $"@{photoUser.User!.TelegramUsername!.TrimStart('@')}",
                City = photoUser.CityNomination?.Value ?? string.Empty,
                AgeNomination = photoUser.AgeNomination,
                GenderNomination = photoUser.GenderNomination,
                Rating = photoUser.Rating,
                RatingCount = photoUser.RatingCount,
                Rank = photoUser.Rank.ToString(),
                CreatedAt = photoUser.CreatedAt,
                TelegramFileIds = files
            };
        }

        private static T? TryDeserialize<T>(string snapshotJson)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(snapshotJson, Options);
            }
            catch
            {
                return default;
            }
        }
    }
}
