using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Domain.Base
{
    public abstract class DomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public Guid AggregateId { get; protected set; }
        public string EventType => GetType().Name;
        public int Version {get; private set;}

        public void UpdateVersion(int value) => Version = value + 1;
        protected DomainEvent() { }
    }
}