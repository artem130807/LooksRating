using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class UserConfigurations : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TelegramId)
                .IsRequired();

            builder.HasIndex(x => x.TelegramId)
                .IsUnique();

            builder.Property(x => x.TelegramUsername)
                .HasMaxLength(32);

            builder.Property(x => x.Name)
                .HasMaxLength(32);

            builder.HasMany(u => u.PhotoUsers)
                .WithOne()
                .HasForeignKey(u => u.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
