namespace LooksRatingApi.CQRS.Users.Query.GetUserStats
{
    public sealed class GetUserStatsResponse
    {
        public long TelegramId { get; init; }
        public int TimesInTop { get; init; }
        public int SeasonsWithPhoto { get; init; }
    }
}
