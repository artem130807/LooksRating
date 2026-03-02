using System;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class ListSeasonsConfigurations : IEntityTypeConfiguration<ListSeasons>
    {
        public void Configure(EntityTypeBuilder<ListSeasons> builder)
        {
            builder.ToTable("ListSeasons");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.CreatedDate)
                   .IsRequired();
        }
    }
}

