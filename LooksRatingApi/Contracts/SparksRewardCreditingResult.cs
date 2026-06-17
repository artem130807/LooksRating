namespace LooksRatingApi.Contracts
{
    public sealed record SparksRewardCreditingResult(
        int Credited,
        int Skipped,
        int NotFound,
        int Failed);
}
