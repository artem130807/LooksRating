using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class UserSessionConfigurations : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.ToTable("UserSession");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.TelegramId)
                   .IsRequired();

            builder.HasIndex(s => s.TelegramId);

            builder.Property(s => s.State)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(s => s.UpdatedAt)
                   .IsRequired();

            builder.HasOne(s => s.User)
                   .WithMany()
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}