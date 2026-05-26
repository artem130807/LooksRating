namespace LooksRatingApi.Contracts.SeasonLifecycle
{
    public interface INewSeasonProcessor
    {
        Task ProcessMonthlyRolloverAsync(CancellationToken cancellationToken);
    }
}
