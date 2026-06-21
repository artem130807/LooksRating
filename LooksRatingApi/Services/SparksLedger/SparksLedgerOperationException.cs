namespace LooksRatingApi.Services.SparksLedger;

public sealed class SparksLedgerOperationException : Exception
{
    public SparksLedgerOperationException(string message)
        : base(message)
    {
    }
}
