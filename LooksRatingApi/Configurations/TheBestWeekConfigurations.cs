using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class TheBestWeekConfigurations : IEntityTypeConfiguration<TheBestWeek>
    {
        public void Configure(EntityTypeBuilder<TheBestWeek> builder)
        {
            builder.ToTable("TheBestWeek");
            builder.HasKey(b => b.Id);
            builder.ComplexProperty(c => c.City, c =>
            {
                c.IsRequired();
                c.Property(e => e.Value)
                 .HasColumnName("City")
                 .HasMaxLength(255);
            });
            builder.Property(b => b.CreatedDate)
                   .IsRequired();
            builder.HasMany(b => b.PhotoUsers)
                   .WithOne()
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}