namespace LooksRatingApi.Contracts
{
    public enum ReferralInviteReservationStatus
    {
        Reserved = 0,
        LimitReached = 1,
        AlreadyInvited = 2,
        ReferrerLinkNotFound = 3,
    }

    public sealed record ReferralInviteReservationResult(
        ReferralInviteReservationStatus Status,
        int InvitedCount = 0);
}
