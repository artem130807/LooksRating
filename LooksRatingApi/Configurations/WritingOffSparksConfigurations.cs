using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class WritingOffSparksConfigurations : IEntityTypeConfiguration<WritingOffSparks>
    {
        public void Configure(EntityTypeBuilder<WritingOffSparks> builder)
        {
            builder.ToTable("WritingOffSparks");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status);
            builder.Property(x => x.City).IsRequired();
            builder.Property(x => x.SparksCount);
            builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Stars);
            builder.Property(x => x.CreatedAt);
            builder.HasIndex(x => new { x.City, x.Status });
            builder.HasOne(x => x.User)
            .WithMany(x => x.WritingOffSparks)
            .HasForeignKey(x => x.UserId);
            builder.HasIndex(w => new { w.UserId, w.IdempotencyKey })
               .IsUnique()
               .HasDatabaseName("IX_Withdrawals_UserId_IdempotencyKey");
        }
    }
}