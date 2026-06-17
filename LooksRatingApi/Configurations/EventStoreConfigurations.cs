using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class EventStoreConfigurations : IEntityTypeConfiguration<EventStore>
    {
        public void Configure(EntityTypeBuilder<EventStore> builder)
        {
            builder.ToTable("EventStores");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AggregateId).IsRequired();
            builder.Property(x => x.EventType).IsRequired().HasMaxLength(256);
            builder.Property(x => x.EventData).IsRequired();
            builder.Property(x => x.Version).IsRequired();
            builder.Property(x => x.OccurredAt).IsRequired();

            builder.HasIndex(x => new { x.AggregateId, x.Version }).IsUnique();
            builder.HasIndex(x => x.OccurredAt);
        }
    }
}
