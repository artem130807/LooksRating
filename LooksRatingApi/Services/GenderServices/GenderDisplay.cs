using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Services
{
    public class GenderDisplay
    {
        private static readonly Dictionary<GenderEnum, string> _gender = new()
        {
            { GenderEnum.Male, "Мужской" },
            { GenderEnum.Female, "Женский" },
            { GenderEnum.Unknown, "Не указан"},
            { GenderEnum.MaleFamale, "Оба" },
        };

        public static string GetGender(GenderEnum gender) => _gender[gender];
    }
}