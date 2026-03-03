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
                   .HasColumnType("decimal(4,2)");

            builder.Property(p => p.RatingCount)
                   .IsRequired();
                   
            builder.HasOne(p => p.User)
                   .WithOne(u => u.PhotoUser)
                   .HasForeignKey<User>(u => u.PhotoUserId);
        }
    }
}