using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Contracts
{
    public interface ISparksLedgerEventDispatcher
    {
        Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken);
    }
}
