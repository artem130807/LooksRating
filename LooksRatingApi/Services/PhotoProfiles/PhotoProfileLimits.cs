using LooksRatingApi.Enums;

namespace LooksRatingApi.Services.PhotoProfiles
{
    public static class PhotoProfileLimits
    {
        public const int VipMaxPhotos = 4;
        public const int NonVipMaxPhotos = 1;

        public static int GetMaxPhotos(VipStatus vipStatus) =>
            vipStatus == VipStatus.Availlable ? VipMaxPhotos : NonVipMaxPhotos;

        public static bool CanAddPhoto(int currentPhotoCount, VipStatus vipStatus) =>
            currentPhotoCount < GetMaxPhotos(vipStatus);
    }
}
