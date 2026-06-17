using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Services.PhotoProfiles
{
    public readonly record struct PhotoProfileNomination(int Age, GenderEnum Gender, string City)
    {
        public static PhotoProfileNomination From(PhotoProfile profile) =>
            From(
                profile.AgeNomination,
                profile.GenderNomination,
                profile.CityNomination.Value ?? string.Empty);

        public static PhotoProfileNomination From(int age, GenderEnum gender, string city) =>
            new(age, gender, city.Trim().ToLowerInvariant());

        public bool Matches(PhotoProfileNomination other) =>
            Age == other.Age
            && Gender == other.Gender
            && string.Equals(
                City.Trim().ToLowerInvariant(),
                other.City.Trim().ToLowerInvariant(),
                StringComparison.Ordinal);
    }
}
