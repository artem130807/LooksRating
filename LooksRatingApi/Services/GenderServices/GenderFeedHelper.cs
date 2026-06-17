using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services
{
    public static class GenderFeedHelper
    {
        public static bool Matches(GenderEnum preference, GenderEnum photoGender) =>
            preference switch
            {
                GenderEnum.MaleFamale => photoGender is GenderEnum.Male or GenderEnum.Female,
                _ => preference == photoGender,
            };

        public static bool Matches(GenderEnum preference, string? photoGenderValue)
        {
            if (string.IsNullOrWhiteSpace(photoGenderValue))
            {
                return false;
            }

            if (!Enum.TryParse<GenderEnum>(photoGenderValue, true, out var photoGender))
            {
                return false;
            }

            return Matches(preference, photoGender);
        }

        public static IQueryable<PhotoUser> ApplyFilter(IQueryable<PhotoUser> query, GenderEnum preference) =>
            preference == GenderEnum.MaleFamale
                ? query.Where(p =>
                    p.GenderNomination == GenderEnum.Male
                    || p.GenderNomination == GenderEnum.Female)
                : query.Where(p => p.GenderNomination == preference);

        public static IQueryable<PhotoProfile> ApplyFilter(IQueryable<PhotoProfile> query, GenderEnum preference) =>
            preference == GenderEnum.MaleFamale
                ? query.Where(p =>
                    p.GenderNomination == GenderEnum.Male
                    || p.GenderNomination == GenderEnum.Female)
                : query.Where(p => p.GenderNomination == preference);
    }
}
