namespace LooksRatingApi.Contracts
{
    public interface IVipStatusExpiryProcessor
    {
        Task ProcessAsync(CancellationToken cancellationToken);
    }
}
