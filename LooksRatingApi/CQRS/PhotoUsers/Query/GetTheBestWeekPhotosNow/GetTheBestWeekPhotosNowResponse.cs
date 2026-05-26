namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow
{
    public sealed class GetTheBestWeekPhotosNowResponse
    {
        public Guid Id { get; set; }
        public int Place { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TelegramFileId { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public string GenderNomination { get; set; } = string.Empty;
        public int AgeNomination { get; set; }
    }
}
