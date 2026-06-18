using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using LooksRatingApi.Services.PhotoProfiles;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto
{
    internal static class GetMyPhotoResponseBuilder
    {
        public static async Task<GetMyPhotoResponse> BuildAsync(
            PhotoProfile profile,
            User user,
            Guid seasonId,
            bool seasonIsClosed,
            IPhotoTopReadService photoTopReadService,
            CancellationToken cancellationToken)
        {
            var seasonTop = await photoTopReadService.GetSeasonTopPositionAsync(
                profile,
                seasonIsClosed,
                cancellationToken);

            return new GetMyPhotoResponse
            {
                ProfileId = profile.Id,
                UserId = user.Id,
                SeasonId = seasonId,
                PhotoCount = profile.Photos.Count,
                MaxPhotos = PhotoProfileLimits.GetMaxPhotos(user.Status),
                CanAddPhoto = PhotoProfileLimits.CanAddPhoto(profile.Photos.Count, user.Status),
                SeasonTopPlace = seasonTop?.Place,
                SeasonTopTotal = seasonTop?.TotalCount,
                Photos = profile.Photos
                    .OrderBy(x => x.SortOrder)
                    .Select(item => new GetMyPhotoItem
                    {
                        Id = item.Id,
                        TelegramFileId = item.TelegramFileId,
                        Rating = profile.Rating,
                        RatingCount = profile.RatingCount,
                        Rank = RankDisplay.GetSticker(profile.Rank),
                        Gender = GenderDisplay.GetGender(profile.GenderNomination),
                        Age = profile.AgeNomination,
                        City = profile.CityNomination.Value ?? string.Empty,
                    })
                    .ToList(),
            };
        }
    }
}
