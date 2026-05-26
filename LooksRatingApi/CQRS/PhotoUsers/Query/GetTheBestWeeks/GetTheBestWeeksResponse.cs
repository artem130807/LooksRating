using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed class GetTheBestWeeksResponse
    {
        public Guid Id { get; set; }
        public string? TelegramUsername { get; set; }
        public string? Name {get; set;}
        public decimal Rating {get; set;}
        public int RatingCount {get; set;}
    }
}
