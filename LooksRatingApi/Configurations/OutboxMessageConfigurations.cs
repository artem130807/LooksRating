using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class OutboxMessageConfigurations : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MessageType)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.PayloadJson)
                .IsRequired();

            builder.Property(x => x.StateJson)
                .IsRequired();

            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.Attempts).IsRequired();
            builder.Property(x => x.LastError).HasMaxLength(2000);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();

            builder.HasIndex(x => new { x.MessageType, x.Status, x.NextAttemptAt })
                .HasDatabaseName("IX_OutboxMessages_Type_Status_NextAttemptAt");
        }
    }
}
