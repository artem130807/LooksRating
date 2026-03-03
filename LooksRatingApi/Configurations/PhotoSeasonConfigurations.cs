using System;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class PhotoSeasonConfigurations : IEntityTypeConfiguration<PhotoSeason>
    {
        public void Configure(EntityTypeBuilder<PhotoSeason> builder)
        {
            builder.ToTable("PhotoSeason");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TelegramFileId)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(p => p.Rank)
                   .HasMaxLength(64);

            builder.Property(p => p.Rating)
                   .HasColumnType("decimal(4,2)");

            builder.Property(p => p.RatingCount)
                   .IsRequired();

            builder.Property(p => p.SnapshotAt)
                   .IsRequired();

            builder.HasOne(p => p.Season)
                   .WithMany(s => s.PhotoSeasons)
                   .HasForeignKey(p => p.SeasonId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.User)
                   .WithMany(u => u.PhotoSeasons)
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

