namespace LooksRatingApi.Contracts
{
    public interface ISeasonTopSparksRewardProcessor
    {
        Task<SeasonTopSparksRewardResult> ProcessForSeasonAsync(
            Guid seasonId,
            bool seasonIsClosed,
            CancellationToken cancellationToken = default);
    }

    public sealed record SeasonTopSparksRewardResult(
        int Credited,
        int Skipped,
        int NotFound,
        int Failed);
}
