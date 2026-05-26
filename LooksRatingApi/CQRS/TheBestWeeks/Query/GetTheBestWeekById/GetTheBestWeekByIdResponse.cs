using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeekById
{
    public sealed class GetTheBestWeekByIdResponse
    {
        public Guid Id { get; set; }
        public string City { get; set; } = string.Empty;
        public int Year { get; set; }
        public int WeekOfYear { get; set; }
        public WeekEnum Week { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<GetTheBestWeekByIdPhotoItemResponse> Photos { get; set; } = [];
    }

    public sealed class GetTheBestWeekByIdPhotoItemResponse
    {
        public Guid Id { get; set; }
        public string TelegramFileId { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public string Rank { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int AgeNomination { get; set; }
        public GenderEnum GenderNomination { get; set; }
    }
}
