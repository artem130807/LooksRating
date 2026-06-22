namespace LooksRatingApi.Services.TheBestWeek
{
    public sealed record TheBestWeekWeekRecord(string City, IReadOnlyList<TheBestWeekSnapshotItem> SnapshotItems);
}
