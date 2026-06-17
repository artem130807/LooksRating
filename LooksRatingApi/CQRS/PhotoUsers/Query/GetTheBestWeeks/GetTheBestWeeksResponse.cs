using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed class GetTheBestWeeksResponse
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public int Place { get; set; }
        public string? TelegramUsername { get; set; }
        public string? Name {get; set;}
        public string TelegramFileId { get; set; } = string.Empty;
        public IReadOnlyList<string> TelegramFileIds { get; set; } = Array.Empty<string>();
        public string GenderNomination { get; set; } = string.Empty;
        public int AgeNomination { get; set; }
        public decimal Rating {get; set;}
        public int RatingCount {get; set;}
    }
}
