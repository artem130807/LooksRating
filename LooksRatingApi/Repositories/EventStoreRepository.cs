using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class EventStoreRepository:IEventStoreRepository
    {
        private readonly LooksRatingDbContext _context;
        public EventStoreRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DomainEvent>> GetAllEventsAsync()
        {
            var events = await _context.EventStores.OrderBy(x => x.Version).ToListAsync();
            return events.Select(x => x.Desiriallize());
        }

        public async Task<IEnumerable<DomainEvent>> GetEventsAfterDateAsync(DateTime afterDate)
        {
            var entities = await _context.EventStores.Where(x => x.OccurredAt == afterDate).OrderBy(x => x.Version).ToListAsync();
            return entities.Select(x => x.Desiriallize());
        }

        public async Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId)
        {
            var entities = await _context.EventStores.Where(x => x.AggregateId == aggregateId).OrderBy(x => x.Version).ToListAsync();
            return entities.Select(x => x.Desiriallize());
        }

        public async Task<DomainEvent> GetLastEvent(Guid aggregateId)
        {
            var entity = await _context.EventStores
            .Where(x => x.AggregateId == aggregateId)
            .OrderByDescending(x => x.Version) 
            .FirstOrDefaultAsync();             
            
            if (entity == null)
            return null;
    
            return entity.Desiriallize();
        }

        public async Task<int> GetLastVersion(Guid SalonId)
        {
            var lastEvent = await _context.EventStores
            .Where(x => x.AggregateId == SalonId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync();
            
            return lastEvent?.Version ?? 0;
        }

        public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events)
        {
            var lastVersion = await GetLastVersion(aggregateId);
            var version = lastVersion;
            
            var entities = new List<EventStore>();
            
            foreach (var @event in events)
            {
                @event.UpdateVersion(version);
                version = @event.Version;

                var entity = EventStore.Create(@event);
                entities.Add(entity);
            }

            await _context.EventStores.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
        public async Task<DomainEvent> GetPreviousEvent(Guid aggregateId, DateTime eventTime)
        {
            return await _context.EventStores
                .Where(x => x.AggregateId == aggregateId && x.OccurredAt < eventTime)
                .OrderByDescending(x => x.OccurredAt)
                .Select(x => x.Desiriallize())
                .FirstOrDefaultAsync();
        }

    }
}