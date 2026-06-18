using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos;
using LooksRatingApi.Models;
using LooksRatingApi.Services;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos
{
    internal static class GetUserPhotosResponseBuilder
    {
        public static async Task<GetUserPhotosResponse> BuildAsync(
            PhotoProfile profile,
            IReadOnlyList<GetUserPhotosItem> photos,
            bool seasonIsClosed,
            IPhotoProfileRepository photoProfileRepository,
            CancellationToken cancellationToken)
        {
            var seasonTop = await photoProfileRepository.GetSeasonTopPositionAsync(
                profile,
                seasonIsClosed,
                cancellationToken);

            return new GetUserPhotosResponse
            {
                ProfileId = profile.Id,
                Rank = RankDisplay.GetSticker(profile.Rank),
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
                UserId = profile.UserId,
                Gender = GenderDisplay.GetGender(profile.GenderNomination),
                Age = profile.AgeNomination,
                City = profile.CityNomination.Value ?? string.Empty,
                DisplayName = UserPublicDisplayName.Resolve(profile.User),
                SeasonTopPlace = seasonTop?.Place,
                SeasonTopTotal = seasonTop?.TotalCount,
                Photos = photos.ToList(),
            };
        }
    }
}
