using LooksRatingApi.Enums;
using LooksRatingApi.Services.PhotoProfiles;

namespace LooksRatingApi.Tests.Unit.Services.PhotoProfiles;

public sealed class PhotoProfileLimitsTests
{
    [Theory]
    [InlineData(VipStatus.Availlable, 0, true)]
    [InlineData(VipStatus.Availlable, 3, true)]
    [InlineData(VipStatus.Availlable, 4, false)]
    [InlineData(VipStatus.Availlable, 5, false)]
    [InlineData(VipStatus.Unavaillable, 0, true)]
    [InlineData(VipStatus.Unavaillable, 1, false)]
    public void CanAddPhoto_RespectsVipAndNonVipLimits(
        VipStatus vipStatus,
        int currentCount,
        bool expected)
    {
        PhotoProfileLimits.CanAddPhoto(currentCount, vipStatus).Should().Be(expected);
    }

    [Fact]
    public void GetMaxPhotos_ReturnsFourForVipAndOneOtherwise()
    {
        PhotoProfileLimits.GetMaxPhotos(VipStatus.Availlable).Should().Be(4);
        PhotoProfileLimits.GetMaxPhotos(VipStatus.Unavaillable).Should().Be(1);
    }
}
