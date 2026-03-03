using System;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class SeasonConfigurations : IEntityTypeConfiguration<Season>
    {
        public void Configure(EntityTypeBuilder<Season> builder)
        {
            builder.ToTable("Season");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.Property(s => s.Number)
                   .IsRequired();

            builder.Property(s => s.IsClosed)
                   .IsRequired();

            builder.Property(s => s.CreatedDate)
                   .IsRequired();

            builder.HasOne(s => s.ListSeasons)
                   .WithMany(l => l.Seasons)
                   .HasForeignKey(s => s.ListSeasonsId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.PhotoSeasons)
                   .WithOne(p => p.Season)
                   .HasForeignKey(p => p.SeasonId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

