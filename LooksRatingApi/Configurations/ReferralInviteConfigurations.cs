using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LooksRatingApi.Configurations
{
    public sealed class ReferralInviteConfigurations : IEntityTypeConfiguration<ReferralInvite>
    {
        public void Configure(EntityTypeBuilder<ReferralInvite> builder)
        {
            builder.ToTable("ReferralInvite");
            builder.HasKey(invite => invite.Id);

            builder.Property(invite => invite.ReferrerUserId).IsRequired();
            builder.Property(invite => invite.InvitedUserId).IsRequired();
            builder.Property(invite => invite.CreatedAt).IsRequired();

            builder.HasIndex(invite => invite.InvitedUserId).IsUnique();
            builder.HasIndex(invite => invite.ReferrerUserId);
        }
    }
}
