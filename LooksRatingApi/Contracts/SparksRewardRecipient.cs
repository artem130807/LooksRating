namespace LooksRatingApi.Contracts
{
    public sealed record SparksRewardRecipient(
        long TelegramId,
        int Place,
        decimal SparksAmount,
        string Payload);
}
