using System.Text.Json.Serialization;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Domain.DomainEvents
{
    public sealed class CurrencyDebitCompensatedEvent : DomainEvent
    {
        public decimal NewSparksCount { get; set; }
        public decimal CompensatedAmount { get; set; }
        public Guid OriginalEventId { get; set; }
        public string CompensationReason { get; set; } = string.Empty;

        [JsonConstructor]
        private CurrencyDebitCompensatedEvent()
        {
        }

        public CurrencyDebitCompensatedEvent(
            Guid sparksLedgerId,
            decimal newSparksCount,
            decimal compensatedAmount,
            Guid originalEventId,
            string reason)
        {
            AggregateId = sparksLedgerId;
            NewSparksCount = newSparksCount;
            CompensatedAmount = compensatedAmount;
            OriginalEventId = originalEventId;
            CompensationReason = reason;
        }
    }
}
