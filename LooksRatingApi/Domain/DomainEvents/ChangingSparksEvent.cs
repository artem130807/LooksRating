using System.Text.Json.Serialization;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Domain.DomainEvents
{
    public class CurrencySparksEvent : DomainEvent
    {
        public decimal SparksCount { get; set; }

        [JsonConstructor]
        private CurrencySparksEvent()
        {
        }

        public CurrencySparksEvent(Guid sparksLedgerId, decimal sparksCount)
        {
            AggregateId = sparksLedgerId;
            SparksCount = sparksCount;
        }
    }
}
