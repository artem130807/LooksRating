using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Contracts
{
    public interface IChangeSparksLedgersService
    {
        Task ApplySparksCreditAsync(CurrencySparksEvent @event, CancellationToken cancellationToken);

        Task ApplyDebitCompensationAsync(
            CurrencyDebitCompensatedEvent @event,
            CancellationToken cancellationToken);
    }
}