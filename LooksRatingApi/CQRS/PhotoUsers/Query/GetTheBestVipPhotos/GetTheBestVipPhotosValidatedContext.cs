using LooksRatingApi.Models;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestVipPhotos
{
    public sealed class GetTheBestVipPhotosValidatedContext
    {
        public const int TopPhotoCount = 10;

        public required GetTheBestVipPhotosQuery Query { get; init; }
        public required Season CurrentSeason { get; init; }
        public required string FeedCity { get; init; }
    }
}
