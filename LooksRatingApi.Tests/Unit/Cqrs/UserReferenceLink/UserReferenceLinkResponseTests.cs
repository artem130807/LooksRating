using LooksRatingApi.CQRS.UserReferenceLink;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.Tests.Unit.Cqrs.UserReferenceLink;

public sealed class UserReferenceLinkResponseTests
{
    [Fact]
    public void FromModel_MapsInviteStats()
    {
        var userId = Guid.NewGuid();
        var link = LooksRatingApi.Models.UserReferenceLink.Create(userId).Value;
        link.AddCountInvited();

        var response = UserReferenceLinkResponse.FromModel(link);

        response.Link.Should().Be(link.Link);
        response.CountInvited.Should().Be(1);
        response.MaxInvited.Should().Be(ReferralSparksRules.MaxInvitedUsers);
    }

    [Fact]
    public void ToApiPayload_UsesCamelCaseKeys()
    {
        var response = new UserReferenceLinkResponse("https://t.me/bot?start=1", 2, 5);

        var payload = response.ToApiPayload();

        payload.Should().BeEquivalentTo(new
        {
            link = "https://t.me/bot?start=1",
            countInvited = 2,
            maxInvited = 5,
        });
    }
}
