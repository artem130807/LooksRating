using LooksRatingApi.Models;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow
{
    public sealed class GetTheBestWeekPhotosNowValidatedContext
    {
        public const int TopPhotoCount = 10;

        public required GetTheBestWeekPhotosNowQuery Query { get; init; }
        public required Season CurrentSeason { get; init; }
        public required string FeedCity { get; init; }
    }
}
