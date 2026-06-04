namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed class GetTheBestWeeksRequest
    {
        public long? TelegramId { get; set; }
        public LooksRatingApi.Enums.GenderEnum GenderEnum { get; set; }
        public int Age { get; set; }
    }
}
