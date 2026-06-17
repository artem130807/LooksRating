namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface ISparksWalletProvisioner
    {
        Task EnsureForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
