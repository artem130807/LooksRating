namespace LooksRatingApi.Contracts.SeasonLifecycle
{
    public interface INewListSeasonProcessor
    {
        Task<bool> TryCreateNewChapterAsync(CancellationToken cancellationToken);
    }
}
