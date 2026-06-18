using LooksRatingApi.Models;
using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.CQRS.UserReferenceLink
{
    public sealed record UserReferenceLinkResponse(
        string Link,
        int CountInvited,
        int MaxInvited)
    {
        public static UserReferenceLinkResponse FromModel(Models.UserReferenceLink link) =>
            new(
                link.Link,
                link.CountInvited,
                ReferralSparksRules.MaxInvitedUsers);

        public object ToApiPayload() => new
        {
            link = Link,
            countInvited = CountInvited,
            maxInvited = MaxInvited,
        };
    }
}
