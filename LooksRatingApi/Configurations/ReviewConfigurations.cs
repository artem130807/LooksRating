using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class ReviewConfigurations : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Review", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_Review_Rating_Range", "\"Rating\" >= 1 AND \"Rating\" <= 10");
            });
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Rating)
                   .IsRequired();

            builder.HasIndex(r => new { r.UserId, r.PhotoUserId })
                   .IsUnique();

            builder.HasOne(r => r.User)
                   .WithMany(u => u.Reviews)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.PhotoUser)
                   .WithMany()
                   .HasForeignKey(r => r.PhotoUserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}