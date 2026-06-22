using CSharpFunctionalExtensions;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Services;

namespace LooksRatingApi.Models
{
    public class RecomendationSettings
    {
        public Guid Id { get; set; }
        public int? Age { get; set; }
        public GenderEnum Gender { get; set; }
        public CityVo City { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public static Result<RecomendationSettings> Create(int age, GenderEnum genderEnum, CityVo city, Guid userId)
        {
            return new RecomendationSettings
            {
                Id = Guid.NewGuid(),
                Age = age,
                Gender = genderEnum,
                City = city,
                UserId = userId
            };
        }

        public void UpdateAge(int age) => Age = age;

        public void UpdateGender(GenderEnum genderEnum) => Gender = genderEnum;

        public void UpdateCity(CityVo city) => City = city;

        public bool IsComplete =>
            Age is int ageValue
            && TopService.IsValidFeedAge(ageValue)
            && Enum.IsDefined(typeof(GenderEnum), Gender)
            && Gender != GenderEnum.Unknown
            && !string.IsNullOrWhiteSpace(City?.Value);
    }
}
