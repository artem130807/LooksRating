using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Domain.DomainEvents
{
    public class CreateSparksLedgerEvent:DomainEvent
    {
        public Guid UserId { get; private set; }
        public decimal SparksCount { get; private set; }
        public string IdempotencyKey { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }

        [JsonConstructor]
        private CreateSparksLedgerEvent()
        {
            
        }
        public CreateSparksLedgerEvent(
            Guid sparksLedgerId,
            Guid userId,
            string idempotencyKey,
            decimal sparksCount = 0m)
        {
            AggregateId = sparksLedgerId;
            UserId = userId;
            IdempotencyKey = idempotencyKey;
            SparksCount = sparksCount;
            CreatedAt = DateTime.UtcNow;
        }
    }
}