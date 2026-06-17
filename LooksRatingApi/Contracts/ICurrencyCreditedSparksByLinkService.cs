namespace LooksRatingApi.Contracts
{
    public interface ICurrencyCreditedSparksByLinkService
    {
        Task CreditReferrerForRegistrationAsync(
            Guid newUserId,
            string? referralLink,
            CancellationToken cancellationToken = default);
    }
}
