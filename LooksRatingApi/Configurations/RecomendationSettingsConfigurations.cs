using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class RecomendationSettingsConfigurations : IEntityTypeConfiguration<RecomendationSettings>
    {
        public void Configure(EntityTypeBuilder<RecomendationSettings> builder)
        {
            builder.ToTable("RecomendationSettings");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Age);

            builder.Property(x => x.Gender)
                .IsRequired();

            builder.ComplexProperty(c => c.City, c =>
            {
                c.IsRequired();
                c.Property(e => e.Value)
                    .HasColumnName("City")
                    .HasMaxLength(255);
            });

            builder.HasIndex(x => x.UserId)
                .IsUnique();

            builder.HasOne(x => x.User)
                .WithOne(u => u.RecomendationSettings)
                .HasForeignKey<RecomendationSettings>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
