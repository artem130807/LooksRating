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

            builder.Property(b => b.City)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(b => b.Year)
                .IsRequired();

            builder.Property(b => b.WeekOfYear)
                .IsRequired();

            builder.Property(b => b.Week)
                .IsRequired();

            builder.Property(b => b.CreatedDate)
                .IsRequired();

            builder.HasIndex(b => new { b.City, b.Year, b.WeekOfYear })
                .IsUnique();
        }
    }
}
