using System.Text.Json.Serialization;

namespace LooksRatingApi.Domain.Base
{
    public abstract class DomainEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        [JsonInclude]
        public Guid AggregateId { get; set; }

        public string EventType => GetType().Name;
        public int Version { get; set; }

        public void UpdateVersion(int value) => Version = value + 1;
        protected DomainEvent() { }
    }
}
