using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class PaymentOrderConfigurations : IEntityTypeConfiguration<PaymentOrder>
    {
        public void Configure(EntityTypeBuilder<PaymentOrder> builder)
        {
            builder.ToTable("PaymentOrder");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.ProductId)
                .IsRequired();

            builder.Property(x => x.Payload)
                .IsRequired()
                .HasMaxLength(128);

            builder.HasIndex(x => x.Payload)
                .IsUnique();

            builder.Property(x => x.AmountStars)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.TelegramPaymentChargeId)
                .HasMaxLength(128);

            builder.Property(x => x.ProviderPaymentChargeId)
                .HasMaxLength(128);

            builder.HasIndex(x => x.TelegramPaymentChargeId)
                .IsUnique()
                .HasFilter("\"TelegramPaymentChargeId\" IS NOT NULL");

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            builder.Property(x => x.PaidAt);
            builder.Property(x => x.FailedAt);
            builder.Property(x => x.CancelledAt);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(512);

            builder.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
