namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed class GetTheBestWeeksRequest
    {
        public long? TelegramId { get; set; }
        public string? City { get; set; }
        public int? Year { get; set; }
        public int? WeekOfYear { get; set; }
        public int Limit { get; set; } = 12;
    }
}
