namespace LooksRatingApi.Models
{
    public sealed class ReferralInvite
    {
        public Guid Id { get; private set; }

        public Guid ReferrerUserId { get; private set; }

        public Guid InvitedUserId { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public static ReferralInvite Create(Guid referrerUserId, Guid invitedUserId)
        {
            return new ReferralInvite
            {
                Id = Guid.NewGuid(),
                ReferrerUserId = referrerUserId,
                InvitedUserId = invitedUserId,
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}
