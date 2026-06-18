namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto
{
    public sealed class GetMyPhotoItem
    {
        public Guid Id { get; init; }
        public string TelegramFileId { get; init; } = string.Empty;
        public decimal Rating { get; init; }
        public int RatingCount { get; init; }
        public string Rank { get; init; } = string.Empty;
        public string Gender { get; init; } = string.Empty;
        public int Age { get; init; }
        public string City { get; init; } = string.Empty;
    }

    public sealed class GetMyPhotoResponse
    {
        public Guid ProfileId { get; init; }
        public Guid UserId { get; init; }
        public Guid SeasonId { get; init; }
        public int PhotoCount { get; init; }
        public int MaxPhotos { get; init; }
        public bool CanAddPhoto { get; init; }
        public int? SeasonTopPlace { get; init; }
        public int? SeasonTopTotal { get; init; }
        public List<GetMyPhotoItem> Photos { get; init; } = new();
    }
}
