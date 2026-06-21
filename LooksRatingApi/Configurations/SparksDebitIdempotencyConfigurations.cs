using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations;

public sealed class SparksDebitIdempotencyConfigurations : IEntityTypeConfiguration<SparksDebitIdempotency>
{
    public void Configure(EntityTypeBuilder<SparksDebitIdempotency> builder)
    {
        builder.ToTable("SparksDebitIdempotency");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(128);
        builder.Property(x => x.SparksAmount).IsRequired();
        builder.Property(x => x.StarsCount).IsRequired();
        builder.Property(x => x.DebitEventId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("IX_SparksDebitIdempotency_UserId_IdempotencyKey");
    }
}
