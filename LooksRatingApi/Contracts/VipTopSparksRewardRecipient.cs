namespace LooksRatingApi.Contracts
{
    public sealed record VipTopSparksRewardRecipient(
        long TelegramId,
        int Place,
        decimal SparksAmount,
        string CategoryFingerprint);
}
