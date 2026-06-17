using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class PhotoProfileConfigurations : IEntityTypeConfiguration<PhotoProfile>
    {
        public void Configure(EntityTypeBuilder<PhotoProfile> builder)
        {
            builder.ToTable("PhotoProfile");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Rating)
                .HasColumnType("decimal(4,2)")
                .HasDefaultValue(0m);

            builder.Property(p => p.RatingCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.Rank).IsRequired();
            builder.Property(p => p.Status).IsRequired();
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.Property(p => p.GenderNomination).IsRequired();
            builder.Property(p => p.AgeNomination).IsRequired();

            builder.ComplexProperty(c => c.CityNomination, c =>
            {
                c.IsRequired();
                c.Property(e => e.Value)
                    .HasColumnName("City")
                    .HasMaxLength(255);
            });

            builder.HasOne(p => p.User)
                .WithMany(u => u.PhotoProfiles)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Season)
                .WithMany(s => s.PhotoProfiles)
                .HasForeignKey(p => p.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => new { p.UserId, p.SeasonId }).IsUnique();
            builder.HasIndex(p => new { p.SeasonId, p.Status });
            builder.HasIndex(p => new { p.SeasonId, p.GenderNomination, p.AgeNomination });
        }
    }
}
