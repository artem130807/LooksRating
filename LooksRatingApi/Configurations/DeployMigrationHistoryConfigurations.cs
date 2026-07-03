using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class DeployMigrationHistoryConfigurations : IEntityTypeConfiguration<DeployMigrationHistory>
    {
        public void Configure(EntityTypeBuilder<DeployMigrationHistory> builder)
        {
            builder.ToTable("DeployMigrationHistory");
            builder.HasKey(x => x.Name);

            builder.Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.AppliedAt)
                .IsRequired();
        }
    }
}
