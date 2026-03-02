using System;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class TopPhotoSeasonConfigurations : IEntityTypeConfiguration<TopPhotoSeason>
    {
        public void Configure(EntityTypeBuilder<TopPhotoSeason> builder)
        {
            builder.ToTable("TopPhotoSeason");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.GenderEnum)
                   .IsRequired();

            builder.Property(t => t.Place)
                   .IsRequired();

            builder.ComplexProperty(t => t.City, c =>
            {
                c.IsRequired();
                c.Property(e => e.Value)
                 .HasColumnName("City")
                 .HasMaxLength(255);
            });

            builder.HasOne(t => t.PhotoSeason)
                   .WithMany(p => p.TopPhotoSeasons)
                   .HasForeignKey(t => t.PhotoSeasonId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => new { t.PhotoSeasonId, t.GenderEnum })
                   .IsUnique();
        }
    }
}

