namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed class GetTopUserPhotosPagedResponse
    {
        public IReadOnlyList<GetTopUserPhotosResponse> Items { get; init; } = Array.Empty<GetTopUserPhotosResponse>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public Guid SeasonId { get; init; }
        public string SeasonName { get; init; } = string.Empty;
        public int SeasonNumber { get; init; }
        public bool IsCurrentSeason { get; init; }
        public bool IsClosed { get; init; }
    }
}
