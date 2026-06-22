using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.SeasonLifecycle
{
    public interface ISeasonRolloverNotifier
    {
        Task<int> EnqueueForRolloverAsync(
            Season closedSeason,
            Season newSeason,
            CancellationToken cancellationToken = default);
    }
}
