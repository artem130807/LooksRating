namespace LooksRatingApi.Contracts
{
    public interface IVipStatusExtensionService
    {
        Task<VipStatusExtensionResult> ExtendByTelegramIdsAsync(
            IReadOnlyCollection<long> telegramIds,
            Guid seasonId,
            CancellationToken cancellationToken = default);
    }

    public sealed record VipStatusExtensionResult(int Extended, int Skipped, int NotFound);
}
