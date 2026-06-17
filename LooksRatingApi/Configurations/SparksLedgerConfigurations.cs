using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class SparksLedgerConfigurations : IEntityTypeConfiguration<SparksWallet>
    {
        public void Configure(EntityTypeBuilder<SparksWallet> builder)
        {
            builder.ToTable("SparksLedger");
            builder.Ignore("Version");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.SparksCount)
                .IsRequired()
                .HasPrecision(18, 4);

            builder.Property(x => x.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(SparksWallet.IdempotencyKeyMaxLength);

            builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.UserId)
                .IsUnique();

            builder.HasIndex(x => new { x.UserId, x.CreatedAt });

            builder.HasOne(x => x.User)
                .WithOne(x => x.SparksWallet)
                .HasForeignKey<SparksWallet>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
