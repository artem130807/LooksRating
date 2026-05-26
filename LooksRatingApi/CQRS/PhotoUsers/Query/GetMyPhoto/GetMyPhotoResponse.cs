namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto
{
    public sealed class GetMyPhotoResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string TelegramFileId { get; init; } = string.Empty;
        public decimal Rating { get; init; }
        public int RatingCount { get; init; }
        public string Rank { get; init; } = string.Empty;
        public string Gender { get; init; } = string.Empty;
        public int Age { get; init; }
        public string City { get; init; } = string.Empty;
        public Guid SeasonId { get; init; }
    }
}
