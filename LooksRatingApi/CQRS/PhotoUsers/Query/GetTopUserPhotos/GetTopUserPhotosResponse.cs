namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed class GetTopUserPhotosResponse
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public int Place { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TelegramFileId { get; set; } = string.Empty;
        public IReadOnlyList<string> TelegramFileIds { get; set; } = Array.Empty<string>();
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public string GenderNomination { get; set; } = string.Empty;
        public int AgeNomination { get; set; }
    }
}
