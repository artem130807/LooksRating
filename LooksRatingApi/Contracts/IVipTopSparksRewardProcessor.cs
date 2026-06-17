namespace LooksRatingApi.Contracts
{
    public interface IVipTopSparksRewardProcessor
    {
        Task<VipTopSparksRewardResult> ProcessAsync(CancellationToken cancellationToken = default);
    }

    public sealed record VipTopSparksRewardResult(
        int SparksCredited,
        int SparksSkipped,
        int SparksNotFound,
        int SparksFailed,
        int VipExtended,
        int VipSkipped,
        int VipNotFound);
}
