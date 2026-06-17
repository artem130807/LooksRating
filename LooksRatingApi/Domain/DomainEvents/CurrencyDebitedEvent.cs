using System.Text.Json.Serialization;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Domain.DomainEvents
{
    public class CurrencyDebitedEvent : DomainEvent
    {
        public decimal SparksCount { get; set; }

        [JsonConstructor]
        private CurrencyDebitedEvent()
        {
        }

        public CurrencyDebitedEvent(Guid sparksLedgerId, decimal sparksCount)
        {
            AggregateId = sparksLedgerId;
            SparksCount = sparksCount;
        }
    }
}
