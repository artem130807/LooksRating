using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class UserConfigurations : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TelegramId)
                   .IsRequired();

            builder.HasIndex(x => x.TelegramId)
                   .IsUnique();

            builder.Property(x => x.TelegramUsername)
                   .HasMaxLength(32);

            builder.Property(x => x.Age);

            builder.Property(x => x.Gender)
                   .IsRequired();

            builder.Property(x => x.TimesInTop)
                   .HasDefaultValue(0);

            builder.ComplexProperty(c => c.City, c =>
            {
                c.IsRequired();
                c.Property(e => e.Value)
                 .HasColumnName("City")
                 .HasMaxLength(255);
            });


            builder.HasMany(u => u.UserTickets)
                   .WithOne(t => t.User)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Reviews)
                   .WithOne(r => r.User)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}