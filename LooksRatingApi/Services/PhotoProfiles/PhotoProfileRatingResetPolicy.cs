using LooksRatingApi.Enums;

namespace LooksRatingApi.Services.PhotoProfiles
{
    public static class PhotoProfileRatingResetPolicy
    {
        /// <summary>
        /// Non-VIP: reset on any photo recreate.
        /// VIP: reset only when age, gender, or city nomination changes; keep rating when only photos change.
        /// </summary>
        public static bool ShouldResetRating(
            VipStatus vipStatus,
            PhotoProfileNomination currentNomination,
            PhotoProfileNomination requestedNomination)
        {
            if (!currentNomination.Matches(requestedNomination))
            {
                return true;
            }

            return vipStatus != VipStatus.Availlable;
        }
    }
}
