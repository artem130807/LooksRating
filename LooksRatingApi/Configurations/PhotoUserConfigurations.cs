using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class PhotoUserConfigurations : IEntityTypeConfiguration<PhotoUser>
    {
        public void Configure(EntityTypeBuilder<PhotoUser> builder)
        {
            builder.ToTable("PhotoUser");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TelegramFileId)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(p => p.Rating)
                   .HasColumnType("decimal(4,2)")
                   .HasDefaultValue(0m);

            builder.Property(p => p.RatingCount)
                   .IsRequired()
                   .HasDefaultValue(0);
            
            builder.Property(p => p.Rank)
                   .IsRequired();

            builder.Property(p => p.Status)
                   .IsRequired();

            builder.Property(p => p.CreatedAt)
                   .IsRequired();
              
            builder.Property(p => p.GenderNomination)
                   .IsRequired();

            builder.HasOne(p => p.User)
                   .WithMany(p => p.PhotoUsers)
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.ComplexProperty(c => c.CityNomination, c =>
            {
              c.IsRequired();
              c.Property(e => e.Value)
              .HasColumnName("City")
              .HasMaxLength(255);
            });
            builder.Property(x => x.AgeNomination);

            builder.HasOne(p => p.Season)
                   .WithMany(s => s.PhotoUsers)
                   .HasForeignKey(p => p.SeasonId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.PhotoProfile)
                   .WithMany(pp => pp.LegacyPhotoUsers)
                   .HasForeignKey(p => p.PhotoProfileId)
                   .OnDelete(DeleteBehavior.SetNull);
                   
            builder.HasIndex(p => new { p.UserId, p.SeasonId });
        }
    }
}