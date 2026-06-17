using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Base;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace LooksRatingApi.Models
{
    public class EventStore
    {
        public Guid Id {get; private set;}
        public Guid AggregateId {get; private set;}
        public string EventType {get; private set;}
        public string EventData {get; private set;}
        public int Version {get; private set;}
        public DateTime OccurredAt {get; private set;}
        private EventStore(){}
        public static EventStore Create<T>(T domainEvent) where T: DomainEvent
        {
            return new EventStore
            {
                Id = Guid.NewGuid(),
                AggregateId = domainEvent.AggregateId,
                EventType = domainEvent.EventType,
                EventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                Version = domainEvent.Version,
                OccurredAt = domainEvent.OccurredAt  
            };
        }
        public DomainEvent Desiriallize()
        {
            var eventType = Type.GetType(EventType);
            if(eventType == null)
                throw new InvalidOperationException($"Неправильный тип ивента: {EventType}");
            return (DomainEvent)JsonSerializer.Deserialize(EventData, eventType);
        }
    }
}