using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class PhotoProfilePhotoConfigurations : IEntityTypeConfiguration<PhotoProfilePhoto>
    {
        public void Configure(EntityTypeBuilder<PhotoProfilePhoto> builder)
        {
            builder.ToTable("PhotoProfilePhoto");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TelegramFileId)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(p => p.SortOrder)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.HasOne(p => p.PhotoProfile)
                .WithMany(pp => pp.Photos)
                .HasForeignKey(p => p.PhotoProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => p.PhotoProfileId);
            builder.HasIndex(p => new { p.PhotoProfileId, p.SortOrder }).IsUnique();
        }
    }
}
