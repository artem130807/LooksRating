namespace LooksRatingApi.Contracts.TheBestWeekContracts
{
    public interface ITheBestWeekProcessor
    {
        Task RefreshWeeklyAsync(CancellationToken cancellationToken);
    }
}
