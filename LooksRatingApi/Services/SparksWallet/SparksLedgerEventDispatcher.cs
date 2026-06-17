using LooksRatingApi.Contracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Services.SparksLedger
{
    public sealed class SparksLedgerEventDispatcher : ISparksLedgerEventDispatcher
    {
        private readonly IChangeSparksLedgersService _changeSparksLedgersService;

        public SparksLedgerEventDispatcher(IChangeSparksLedgersService changeSparksLedgersService)
        {
            _changeSparksLedgersService = changeSparksLedgersService;
        }

        public Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
        {
            return domainEvent switch
            {
                CurrencySparksEvent sparksEvent =>
                    _changeSparksLedgersService.ApplySparksCreditAsync(sparksEvent, cancellationToken),
                CurrencyDebitCompensatedEvent compensatedEvent =>
                    _changeSparksLedgersService.ApplyDebitCompensationAsync(compensatedEvent, cancellationToken),
                _ => Task.CompletedTask
            };
        }
    }
}
