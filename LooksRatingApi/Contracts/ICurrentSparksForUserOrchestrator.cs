namespace LooksRatingApi.Contracts
{
    public enum ChannelSubscribeBonusStatus
    {
        Unknown = 0,
        Credited = 1,
        AlreadyCredited = 2,
        UserNotFound = 3,
        Failed = 4,
        Eligible = 5,
    }

    public sealed record ChannelSubscribeBonusResult(
        bool Success,
        string Message,
        ChannelSubscribeBonusStatus Status);

    public interface ICurrentSparksForUserOrchestrator
    {
        Task<ChannelSubscribeBonusResult> ProcessAsync(
            long telegramId,
            bool credit,
            CancellationToken cancellationToken = default);
    }
}
