namespace LooksRatingApi.Contracts
{
    public interface ISparksRewardCreditingService
    {
        Task<SparksRewardCreditingResult> CreditAsync(
            IReadOnlyList<SparksRewardRecipient> recipients,
            int productCode,
            string rewardSource,
            CancellationToken cancellationToken = default);
    }
}
