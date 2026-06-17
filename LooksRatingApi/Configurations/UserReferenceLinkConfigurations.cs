using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public class UserReferenceLinkConfigurations : IEntityTypeConfiguration<UserReferenceLink>
    {
        public void Configure(EntityTypeBuilder<UserReferenceLink> builder)
        {
            builder.ToTable("UserReferenceLink");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.CountInvited);
            builder.Property(u => u.Link);
            builder.HasOne(u => u.User).WithOne();
        }
    }
}