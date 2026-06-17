using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class ReviewMilestoneNotificationConfigurations : IEntityTypeConfiguration<ReviewMilestoneNotification>
    {
        public void Configure(EntityTypeBuilder<ReviewMilestoneNotification> builder)
        {
            builder.ToTable("ReviewMilestoneNotifications");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OwnerTelegramId).IsRequired();
            builder.Property(x => x.CycleNumber).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => new { x.PhotoProfileId, x.CycleNumber }).IsUnique();
            builder.HasIndex(x => x.Status);
        }
    }
}
