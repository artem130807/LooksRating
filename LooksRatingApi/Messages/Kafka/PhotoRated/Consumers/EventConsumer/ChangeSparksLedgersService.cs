using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Services.SparksLedger
{
    public sealed class ChangeSparksLedgersService : IChangeSparksLedgersService
    {
        private readonly ISparksLedgerRepository _sparksLedgerRepository;

        public ChangeSparksLedgersService(ISparksLedgerRepository sparksLedgerRepository)
        {
            _sparksLedgerRepository = sparksLedgerRepository;
        }

        public async Task ApplySparksCreditAsync(CurrencySparksEvent @event, CancellationToken cancellationToken)
        {
            var ledger = await _sparksLedgerRepository.GetByAggregateIdAsync(@event.AggregateId, cancellationToken);
            if (ledger is null)
            {
                return;
            }

            await _sparksLedgerRepository.UpdateBalanceAsync(ledger.Id, @event.SparksCount, cancellationToken);
        }

        public async Task ApplyDebitCompensationAsync(
            CurrencyDebitCompensatedEvent @event,
            CancellationToken cancellationToken)
        {
            var ledger = await _sparksLedgerRepository.GetByAggregateIdAsync(@event.AggregateId, cancellationToken);
            if (ledger is null)
            {
                return;
            }

            await _sparksLedgerRepository.UpdateBalanceAsync(ledger.Id, @event.NewSparksCount, cancellationToken);
        }
    }
}
