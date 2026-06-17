namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface ICurrencyDebitCompensatedService
    {
        Task Compensate(
            Guid userId,
            decimal compensatedAmount,
            Guid originalEventId,
            string reason,
            CancellationToken cancellationToken);
    }
}
