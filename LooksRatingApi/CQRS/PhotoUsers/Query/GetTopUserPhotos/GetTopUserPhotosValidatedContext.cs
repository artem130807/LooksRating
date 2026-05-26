using LooksRatingApi.Models;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed class GetTopUserPhotosValidatedContext
    {
        public required GetTopUserPhotosQuery Query { get; init; }
        public required Season Season { get; init; }
        public Season? CurrentSeason { get; init; }
        public required string FeedCity { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Skip { get; init; }
    }
}
