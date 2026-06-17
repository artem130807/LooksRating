using LooksRatingApi.Enums;
using LooksRatingApi.Services.PhotoProfiles;

namespace LooksRatingApi.Tests.Unit.Services.PhotoProfiles;

public sealed class PhotoProfileRatingResetPolicyTests
{
    private static readonly PhotoProfileNomination MoscowMale25 =
        new(25, GenderEnum.Male, "moscow");

    [Fact]
    public void ShouldResetRating_WhenNominationChanged_ReturnsTrueForVip()
    {
        var requested = new PhotoProfileNomination(25, GenderEnum.Male, "spb");

        PhotoProfileRatingResetPolicy.ShouldResetRating(
                VipStatus.Availlable,
                MoscowMale25,
                requested)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldResetRating_WhenNominationChanged_ReturnsTrueForNonVip()
    {
        var requested = new PhotoProfileNomination(26, GenderEnum.Male, "moscow");

        PhotoProfileRatingResetPolicy.ShouldResetRating(
                VipStatus.Unavaillable,
                MoscowMale25,
                requested)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldResetRating_WhenOnlyPhotoChangesForVip_ReturnsFalse()
    {
        PhotoProfileRatingResetPolicy.ShouldResetRating(
                VipStatus.Availlable,
                MoscowMale25,
                MoscowMale25)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldResetRating_WhenOnlyPhotoChangesForNonVip_ReturnsTrue()
    {
        PhotoProfileRatingResetPolicy.ShouldResetRating(
                VipStatus.Unavaillable,
                MoscowMale25,
                MoscowMale25)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldResetRating_WhenGenderChanged_ReturnsTrueForVip()
    {
        var requested = new PhotoProfileNomination(25, GenderEnum.Female, "moscow");

        PhotoProfileRatingResetPolicy.ShouldResetRating(
                VipStatus.Availlable,
                MoscowMale25,
                requested)
            .Should().BeTrue();
    }
}
